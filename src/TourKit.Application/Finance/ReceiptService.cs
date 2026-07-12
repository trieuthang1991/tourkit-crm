using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Finance;

/// <summary>
/// Phiếu thu + công nợ theo đơn (legacy ReceiptVoucher). Công nợ = Order.TotalRevenue − tổng phiếu thu
/// ĐÃ DUYỆT (IsRecognized). Duyệt 1 cấp ở đây; duyệt nhiều cấp xem <see cref="IReceiptApprovalService"/>.
/// </summary>
public sealed class ReceiptService(
    IRepository<ReceiptVoucher> receiptRepo,
    IRepository<Order> orderRepo,
    IRepository<Customer> customerRepo,
    IValidator<CreateReceiptDto> createValidator) : IReceiptService
{
    public async Task<ReceiptDto> CreateAsync(Guid orderId, CreateReceiptDto dto)
    {
        await Validate(createValidator, dto);

        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        var receipt = new ReceiptVoucher
        {
            Code = "RCP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = "Phiếu thu",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = orderId,
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "cash" : dto.PaymentMethod.Trim(),
            Partner = dto.Partner,
            Note = dto.Note,
            Status = 0,           // 0 = chờ duyệt
            IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
        };
        await receiptRepo.AddAsync(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<ReceiptDto> ApproveAsync(Guid receiptId)
    {
        var receipt = await receiptRepo.GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new NotFoundException();
        }

        if (receipt.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        receipt.Status = 1;          // 1 = đã duyệt
        receipt.IsRecognized = true;
        receiptRepo.Update(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<ReceiptDto> RejectAsync(Guid receiptId)
    {
        var receipt = await receiptRepo.GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new NotFoundException();
        }

        if (receipt.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        receipt.Status = 2;          // 2 = từ chối
        receipt.IsRecognized = false;
        receiptRepo.Update(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<IReadOnlyList<ReceiptDto>> ListByOrderAsync(Guid orderId)
    {
        var receipts = await receiptRepo.ListAsync(r => r.OrderId == orderId);
        return receipts.OrderBy(r => r.IssuedAt).Select(Map).ToList();
    }

    public async Task<PagedResult<ReceiptListItemDto>> ListAllAsync(int page, int size, ReceiptListFilter? filter = null)
    {
        var f = filter ?? new ReceiptListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Lọc cột thật (trạng thái, ngày, hình thức, số tiền) ở DB; q (mã phiếu/mã đơn/khách/người nộp) sau khi làm giàu.
        var pm = string.IsNullOrWhiteSpace(f.PaymentMethod) ? null : f.PaymentMethod.Trim();
        var all = await receiptRepo.ListAsync(r =>
            (f.Status == null || r.Status == f.Status) &&
            (f.From == null || r.IssuedAt >= f.From) &&
            (f.To == null || r.IssuedAt <= f.To) &&
            (pm == null || r.PaymentMethod.Contains(pm)) &&
            (f.AmountFrom == null || r.Amount >= f.AmountFrom) &&
            (f.AmountTo == null || r.Amount <= f.AmountTo));

        // Nạp theo lô: mã đơn + tên khách của các đơn liên quan (để danh sách tổng hiển thị được).
        var orderIds = all.Select(r => r.OrderId).ToHashSet();
        var orders = (await orderRepo.ListAsync(o => orderIds.Contains(o.Id)))
            .ToDictionary(o => o.Id, o => o);
        var customerIds = orders.Values.Select(o => o.CustomerId).ToHashSet();
        var customerNames = (await customerRepo.ListAsync(c => customerIds.Contains(c.Id)))
            .ToDictionary(c => c.Id, c => c.FullName);

        var rows = all.Select(r =>
        {
            orders.TryGetValue(r.OrderId, out var order);
            var customerName = order is not null ? customerNames.GetValueOrDefault(order.CustomerId) : null;
            var dto = new ReceiptListItemDto(
                r.Id, r.Code, r.OrderId, order?.Code, customerName,
                r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Status, r.IsRecognized);
            return (r.CreatedAt, Dto: dto);
        });

        bool MatchQ(ReceiptListItemDto d) =>
            kw == null ||
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (d.OrderCode?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.CustomerName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.Partner?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = rows
            .Where(x => MatchQ(x.Dto)
                && (f.BranchId == null || (orders.TryGetValue(x.Dto.OrderId, out var ord) && ord.BranchId == f.BranchId))
                && (f.SalesUserId == null || (orders.TryGetValue(x.Dto.OrderId, out var os) && os.SalesUserId == f.SalesUserId)))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(x => x.Dto).ToList();
        return new PagedResult<ReceiptListItemDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<ReceiptStatsDto> GetStatsAsync()
    {
        var all = await receiptRepo.ListAsync();
        return new ReceiptStatsDto(
            all.Count, all.Sum(r => r.Amount),
            all.Count(r => r.Status == 0), all.Count(r => r.Status == 1), all.Count(r => r.Status == 2));
    }

    public async Task<OrderBalanceDto> GetBalanceAsync(Guid orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        // Chỉ phiếu ĐÃ DUYỆT mới tính vào công nợ (quy tắc ReceiptQueries.Recognized — một chỗ,
        // ở đây lọc in-memory vì IRepository trả IReadOnlyList chứ không phải IQueryable).
        var receipts = await receiptRepo.ListAsync(r => r.OrderId == orderId);
        var paid = receipts.Where(r => r.IsRecognized).Sum(r => r.Amount);

        return new OrderBalanceDto(orderId, order.TotalRevenue, paid, OrderMath.Outstanding(order.TotalRevenue, paid));
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ReceiptDto Map(ReceiptVoucher r) => new(
        r.Id, r.Code, r.OrderId, r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Note, r.Status, r.IsRecognized);
}
