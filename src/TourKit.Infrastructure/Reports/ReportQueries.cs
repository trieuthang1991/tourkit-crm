using Microsoft.EntityFrameworkCore;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Domain;

namespace TourKit.Infrastructure.Reports;

/// <summary>
/// Query phức tạp/nhiều bảng cho 6 báo cáo — dùng <c>AppDbContext</c> trực tiếp (repo riêng theo convention §5).
/// SQLite-safe: GROUP/SUM/COUNT trong SQL, ghép + ORDER BY ở memory (tránh subquery tương quan không dịch được
/// &amp; tránh ORDER BY decimal/DateTimeOffset trên SQLite). Grid lớn → Materialized View (§F4/§I).
/// </summary>
public sealed class ReportQueries(AppDbContext db) : IReportQueries
{
    /// <summary>
    /// Báo cáo công nợ phải thu (legacy MoneyReport CNPT): các đơn còn nợ = TotalRevenue − tổng phiếu thu ĐÃ DUYỆT.
    /// </summary>
    public async Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync()
    {
        // 2 truy vấn top-level (dùng chung ReceiptQueries.Recognized) rồi ghép ở memory —
        // tránh subquery tương quan (không dịch được extension).
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.Code, o.CustomerId, o.TotalRevenue })
            .ToListAsync();

        var paidByOrder = (await db.ReceiptVouchers.Recognized()
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Paid = g.Sum(r => r.Amount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Paid);

        IReadOnlyList<OrderDebtRowDto> rows = orders
            .Select(o => (o, Paid: paidByOrder.GetValueOrDefault(o.Id, 0m)))
            .Select(x => (x.o, x.Paid, Outstanding: OrderMath.Outstanding(x.o.TotalRevenue, x.Paid)))
            .Where(x => x.Outstanding > 0m)
            .OrderByDescending(x => x.Outstanding)
            .Select(x => new OrderDebtRowDto(
                x.o.Id, x.o.Code, x.o.CustomerId, x.o.TotalRevenue, x.Paid, x.Outstanding))
            .ToList();

        return rows;
    }

    /// <summary>
    /// Báo cáo công nợ phải trả NCC (đối xứng OrderDebt): gom theo ProviderId. TotalCost = Σ OrderCost.ActualAmount;
    /// Paid = Σ PaymentVoucher.Amount đã ghi nhận (IsRecognized). Bổ sung PHÂN TUỔI NỢ (aging) của phần còn nợ:
    /// tiền đã trả phân bổ FIFO cho dòng chi phí cũ nhất trước (theo <see cref="Shared.Entities.BaseEntity.CreatedAt"/>),
    /// phần chưa trả của mỗi dòng rơi vào bucket theo tuổi (ngày) của dòng: Current 0–30, D30 31–60, D60 61–90, D90Plus &gt;90.
    /// </summary>
    public async Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync()
    {
        var now = DateTimeOffset.UtcNow;

        // Kéo TỪNG dòng chi phí (kèm ngày) để phân tuổi nợ FIFO ở memory — không GROUP ở SQL nữa vì aging cần ngày.
        var costs = await db.OrderCosts.AsNoTracking()
            .Select(c => new { c.ProviderId, c.ActualAmount, c.CreatedAt })
            .ToListAsync();

        var paid = await db.PaymentVouchers.AsNoTracking()
            .Where(p => p.IsRecognized && p.ProviderId != null)
            .GroupBy(p => p.ProviderId!.Value)
            .Select(g => new { ProviderId = g.Key, Paid = g.Sum(x => x.Amount) })
            .ToListAsync();

        var providers = await db.Providers.AsNoTracking()
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var costsByProvider = costs.GroupBy(c => c.ProviderId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());
        var paidByProvider = paid.ToDictionary(p => p.ProviderId, p => p.Paid);

        var ids = costsByProvider.Keys.Union(paidByProvider.Keys).Distinct();

        IReadOnlyList<ProviderDebtRowDto> rows = ids
            .Select(id =>
            {
                var providerCosts = costsByProvider.GetValueOrDefault(id) ?? [];
                var total = providerCosts.Sum(c => c.ActualAmount);
                var pd = paidByProvider.GetValueOrDefault(id, 0m);
                var name = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id.ToString();

                // Aging FIFO: trừ tiền đã trả vào các dòng chi phí cũ nhất trước; phần chưa trả → bucket theo tuổi dòng.
                decimal current = 0m, d30 = 0m, d60 = 0m, d90plus = 0m;
                var remainingPay = pd;
                foreach (var c in providerCosts)
                {
                    var applied = Math.Min(remainingPay, c.ActualAmount);
                    remainingPay -= applied;
                    var unpaid = c.ActualAmount - applied;
                    if (unpaid <= 0m)
                    {
                        continue;
                    }

                    var ageDays = (now - c.CreatedAt).TotalDays;
                    if (ageDays <= 30)
                    {
                        current += unpaid;
                    }
                    else if (ageDays <= 60)
                    {
                        d30 += unpaid;
                    }
                    else if (ageDays <= 90)
                    {
                        d60 += unpaid;
                    }
                    else
                    {
                        d90plus += unpaid;
                    }
                }

                return new ProviderDebtRowDto(
                    id, name, total, pd, OrderMath.Outstanding(total, pd),
                    current, d30, d60, d90plus);
            })
            .Where(r => r.TotalCost > 0 || r.Paid > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Sổ cái công nợ (drill-down) của 1 NCC: mỗi dòng chi phí (OrderCost — ghi Nợ = ActualAmount) và mỗi phiếu chi
    /// ĐÃ ghi nhận (PaymentVoucher — ghi Có = Amount), xếp theo thời gian, kèm số dư còn lại luỹ kế. Tenant-scoped
    /// tự động qua global query filter của AppDbContext. Ném NotFoundException nếu NCC không thuộc tenant.
    /// </summary>
    public async Task<ProviderTxnHistoryDto> GetProviderTransactionsAsync(Guid providerId)
    {
        var provider = await db.Providers.AsNoTracking()
            .Where(p => p.Id == providerId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync()
            ?? throw new TourKit.Application.Common.NotFoundException("Nhà cung cấp không tồn tại.");

        var costs = await db.OrderCosts.AsNoTracking()
            .Where(c => c.ProviderId == providerId)
            .Select(c => new { c.OrderId, c.ServiceName, c.ActualAmount, c.CreatedAt })
            .ToListAsync();

        var orderIds = costs.Select(c => c.OrderId).Distinct().ToList();
        var orderCodes = (await db.Orders.AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Code })
                .ToListAsync())
            .ToDictionary(o => o.Id, o => o.Code);

        var payments = await db.PaymentVouchers.AsNoTracking()
            .Where(p => p.IsRecognized && p.ProviderId == providerId)
            .Select(p => new { p.Code, p.Amount, p.IssuedAt, p.ReceiverName, p.Note })
            .ToListAsync();

        // Gộp chi phí (Nợ) + phiếu chi (Có) rồi sắp theo thời gian; số dư còn lại luỹ kế = Σ(Nợ − Có) tới dòng đó.
        var entries = new List<(DateTimeOffset Date, string Type, string RefCode, string? Desc, decimal Debit, decimal Credit)>();
        foreach (var c in costs)
        {
            entries.Add((c.CreatedAt, "cost", orderCodes.GetValueOrDefault(c.OrderId, c.OrderId.ToString()),
                c.ServiceName, c.ActualAmount, 0m));
        }

        foreach (var p in payments)
        {
            entries.Add((p.IssuedAt, "payment", p.Code, p.Note ?? p.ReceiverName, 0m, p.Amount));
        }

        decimal running = 0m;
        var txns = entries
            .OrderBy(e => e.Date)
            .Select(e =>
            {
                running += e.Debit - e.Credit;
                return new ProviderTxnDto(e.Date, e.Type, e.RefCode, e.Desc, e.Debit, e.Credit, running);
            })
            .ToList();

        var totalCost = costs.Sum(c => c.ActualAmount);
        var totalPaid = payments.Sum(p => p.Amount);
        var summary = new ProviderTxnSummaryDto(totalCost, totalPaid, OrderMath.Outstanding(totalCost, totalPaid));

        return new ProviderTxnHistoryDto(provider.Id, provider.Name, summary, txns);
    }

    /// <summary>
    /// Báo cáo Dashboard tổng quan (legacy BusinessActivity/HomePage): một object tổng hợp toàn tenant.
    /// Mỗi tổng 1 truy vấn top-level (SUM/COUNT rỗng → 0) — SQLite-safe, không ORDER BY.
    /// </summary>
    public async Task<DashboardSummaryDto> GetDashboardAsync()
    {
        var orderCount = await db.Orders.CountAsync();
        var totalRevenue = await db.Orders.SumAsync(o => o.TotalRevenue);
        var totalReceived = await db.ReceiptVouchers.Recognized().SumAsync(r => r.Amount);
        var totalCost = await db.OrderCosts.SumAsync(c => c.ActualAmount);
        var totalPaid = await db.PaymentVouchers.Where(p => p.IsRecognized).SumAsync(p => p.Amount);

        return new DashboardSummaryDto(
            orderCount,
            totalRevenue, totalReceived, OrderMath.Outstanding(totalRevenue, totalReceived),
            totalCost, totalPaid, OrderMath.Outstanding(totalCost, totalPaid),
            OrderMath.Profit(totalRevenue, totalCost));
    }

    /// <summary>
    /// Báo cáo dòng tiền theo phương thức thanh toán (legacy ListPaymentMethodsCashFlow): gom theo PaymentMethod.
    /// Inflow = Σ phiếu thu đã ghi nhận; Outflow = Σ phiếu chi đã ghi nhận.
    /// </summary>
    public async Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync()
    {
        // 2 truy vấn top-level (dùng chung ReceiptQueries.Recognized) rồi ghép + sort ở memory —
        // tránh ORDER BY decimal trên SQLite.
        var inflow = await db.ReceiptVouchers.Recognized()
            .GroupBy(r => r.PaymentMethod)
            .Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync();
        var outflow = await db.PaymentVouchers.Where(p => p.IsRecognized)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync();

        var methods = inflow.Select(x => x.Method).Union(outflow.Select(x => x.Method)).Distinct();

        IReadOnlyList<CashFlowRowDto> rows = methods
            .Select(m =>
            {
                var i = inflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
                var o = outflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
                return new CashFlowRowDto(m, i, o, i - o);
            })
            .OrderByDescending(r => r.Net)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Báo cáo doanh thu–lợi nhuận theo đơn (legacy ReportTurnover/TurnoverProfit): Cost tính từ OrderCost
    /// (không tin cột denormalized), mirror OrderDebt.
    /// </summary>
    public async Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync()
    {
        // 2 truy vấn top-level rồi ghép ở memory — tránh subquery tương quan (không dịch được extension),
        // và tránh ORDER BY decimal trên SQLite.
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.Code, o.TotalRevenue })
            .ToListAsync();

        var costByOrder = (await db.OrderCosts
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Cost);

        IReadOnlyList<TurnoverRowDto> rows = orders
            .Select(o =>
            {
                var cost = costByOrder.GetValueOrDefault(o.Id, 0m);
                return new TurnoverRowDto(o.Id, o.Code, o.TotalRevenue, cost, OrderMath.Profit(o.TotalRevenue, cost));
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Báo cáo hoa hồng/lợi nhuận theo nhân viên (legacy ReportTurnoverProfit theo user): gom đơn có SalesUserId
    /// theo user, cost từ OrderCost, rate từ CommissionRule (rule đầu tiên của user, mặc định 0 nếu không có).
    /// </summary>
    public async Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync()
    {
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.SalesUserId != null)
            .Select(o => new { o.Id, o.TotalRevenue, UserId = o.SalesUserId!.Value })
            .ToListAsync();

        var costByOrder = (await db.OrderCosts.AsNoTracking()
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Cost);

        var rules = (await db.CommissionRules.AsNoTracking().ToListAsync())
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First().Percentage);

        IReadOnlyList<CommissionByUserRowDto> rows = orders
            .GroupBy(o => o.UserId)
            .Select(g =>
            {
                var turnover = g.Sum(x => x.TotalRevenue);
                var cost = g.Sum(x => costByOrder.GetValueOrDefault(x.Id, 0m));
                var profit = OrderMath.Profit(turnover, cost);
                var rate = rules.GetValueOrDefault(g.Key, 0m);
                var amount = CommissionMath.ShareAmount(profit, rate);
                return new CommissionByUserRowDto(g.Key, turnover, cost, profit, rate, amount);
            })
            .OrderByDescending(r => r.Profit)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Doanh thu/lợi nhuận theo PHÒNG BAN: gom đơn theo phòng ban của sales phụ trách (Order.SalesUserId →
    /// User.DepartmentId). Đơn không gán sales hoặc sales chưa gán phòng ban gộp vào "Chưa phân bổ".
    /// </summary>
    public async Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync()
    {
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.TotalRevenue, o.SalesUserId })
            .ToListAsync();

        var userDepartment = (await db.Users.AsNoTracking()
                .Select(u => new { u.Id, u.DepartmentId })
                .ToListAsync())
            .ToDictionary(u => u.Id, u => u.DepartmentId);

        var departmentName = (await db.Departments.AsNoTracking()
                .Select(d => new { d.Id, d.Name })
                .ToListAsync())
            .ToDictionary(d => d.Id, d => d.Name);

        var costByOrder = (await db.OrderCosts.AsNoTracking()
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Cost);

        IReadOnlyList<TurnoverByDepartmentRowDto> rows = orders
            .GroupBy(o => o.SalesUserId is { } uid ? userDepartment.GetValueOrDefault(uid) : null)
            .Select(g =>
            {
                var turnover = g.Sum(x => x.TotalRevenue);
                var cost = g.Sum(x => costByOrder.GetValueOrDefault(x.Id, 0m));
                var name = g.Key is { } did ? departmentName.GetValueOrDefault(did, "(phòng ban đã xoá)") : "Chưa phân bổ";
                return new TurnoverByDepartmentRowDto(g.Key, name, g.Count(), turnover, cost, OrderMath.Profit(turnover, cost));
            })
            .OrderByDescending(r => r.Profit)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Hiệu suất theo CHI NHÁNH (legacy dashboard "Hiệu suất theo chi nhánh"): gom đơn theo Order.BranchId.
    /// Thực thu = Σ phiếu thu đã ghi nhận của đơn; Còn thiếu = phải thu; Cost từ OrderCost. Đơn chưa gán chi nhánh → "Chưa phân bổ".
    /// </summary>
    public async Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync()
    {
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.TotalRevenue, o.BranchId })
            .ToListAsync();

        var branchName = (await db.Branches.AsNoTracking()
                .Select(b => new { b.Id, b.Name })
                .ToListAsync())
            .ToDictionary(b => b.Id, b => b.Name);

        var costByOrder = (await db.OrderCosts.AsNoTracking()
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Cost);

        var receivedByOrder = (await db.ReceiptVouchers.Recognized()
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Received = g.Sum(x => x.Amount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Received);

        IReadOnlyList<TurnoverByBranchRowDto> rows = orders
            .GroupBy(o => o.BranchId)
            .Select(g =>
            {
                var turnover = g.Sum(x => x.TotalRevenue);
                var received = g.Sum(x => receivedByOrder.GetValueOrDefault(x.Id, 0m));
                var cost = g.Sum(x => costByOrder.GetValueOrDefault(x.Id, 0m));
                var name = g.Key is { } bid ? branchName.GetValueOrDefault(bid, "(chi nhánh đã xoá)") : "Chưa phân bổ";
                return new TurnoverByBranchRowDto(
                    g.Key, name, g.Count(), turnover, received,
                    OrderMath.Outstanding(turnover, received), cost, OrderMath.Profit(turnover, cost));
            })
            .OrderByDescending(r => r.Turnover)
            .ToList();

        return rows;
    }

    /// <summary>Top khách hàng theo doanh thu (legacy dashboard "Top khách hàng trung thành"): gom đơn theo Customer.</summary>
    public async Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10)
    {
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.CustomerId, o.TotalRevenue })
            .ToListAsync();

        var customerName = (await db.Customers.AsNoTracking()
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync())
            .ToDictionary(c => c.Id, c => c.FullName);

        var receivedByOrder = (await db.ReceiptVouchers.Recognized()
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Received = g.Sum(x => x.Amount) })
                .ToListAsync())
            .ToDictionary(x => x.OrderId, x => x.Received);

        IReadOnlyList<TopCustomerRowDto> rows = orders
            .GroupBy(o => o.CustomerId)
            .Select(g =>
            {
                var revenue = g.Sum(x => x.TotalRevenue);
                var received = g.Sum(x => receivedByOrder.GetValueOrDefault(x.Id, 0m));
                return new TopCustomerRowDto(g.Key, customerName.GetValueOrDefault(g.Key, "(khách đã xoá)"), revenue, received);
            })
            .OrderByDescending(r => r.Revenue)
            .Take(top)
            .ToList();

        return rows;
    }

    /// <summary>
    /// KPI phễu kinh doanh (legacy KeyPerformanceIndicator): báo giá → chấp nhận (Status=2) → chuyển đơn
    /// (ConvertedOrderId != null) → thu tiền. Các tỉ lệ 0..1 (FE hiển thị %). Từ dữ liệu sẵn có.
    /// </summary>
    public async Task<KpiSummaryDto> GetKpiSummaryAsync()
    {
        var quoteCount = await db.Quotes.CountAsync();
        var accepted = await db.Quotes.CountAsync(q => q.Status == 2);
        var converted = await db.Quotes.CountAsync(q => q.ConvertedOrderId != null);

        var orderCount = await db.Orders.CountAsync();
        var totalRevenue = await db.Orders.SumAsync(o => o.TotalRevenue);
        var totalReceived = await db.ReceiptVouchers.Recognized().SumAsync(r => r.Amount);

        return new KpiSummaryDto(
            quoteCount, accepted, converted,
            OrderMath.Rate(accepted, quoteCount),       // tỉ lệ chấp nhận = chấp nhận / tổng báo giá
            OrderMath.Rate(converted, accepted),        // tỉ lệ chuyển đơn = chuyển đơn / đã chấp nhận
            orderCount, totalRevenue, OrderMath.Rate(totalRevenue, orderCount),  // giá trị đơn TB
            totalReceived, OrderMath.Rate(totalReceived, totalRevenue));         // tỉ lệ thu
    }
}
