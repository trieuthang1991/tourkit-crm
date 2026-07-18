namespace TourKit.Application.Reports.Dtos;

/// <summary>Một dòng công nợ đơn hàng: tổng phải thu, đã thu (phiếu đã duyệt), còn nợ.</summary>
public sealed record OrderDebtRowDto(
    Guid OrderId, string OrderCode, Guid CustomerId, decimal Total, decimal Paid, decimal Outstanding);

/// <summary>
/// Một dòng công nợ nhà cung cấp: tổng chi phí, đã chi (phiếu đã duyệt), còn phải trả + phân tuổi nợ (aging).
/// Aging = phần CÒN NỢ chia theo tuổi (ngày) của dòng chi phí gốc: <c>Current</c> 0–30 ngày, <c>D30</c> 31–60,
/// <c>D60</c> 61–90, <c>D90Plus</c> &gt;90. Tuổi tính từ <c>OrderCost.CreatedAt</c> so với hiện tại; tiền đã trả
/// phân bổ FIFO cho dòng cũ nhất trước, phần chưa trả của mỗi dòng rơi vào bucket theo tuổi của nó.
/// Tổng 4 bucket = max(0, Outstanding). Các trường aging THÊM (additive) — không phá contract cũ.
/// </summary>
public sealed record ProviderDebtRowDto(
    Guid ProviderId, string ProviderName, decimal TotalCost, decimal Paid, decimal Outstanding,
    decimal Current, decimal D30, decimal D60, decimal D90Plus);

/// <summary>Một dòng sổ cái công nợ NCC: chi phí (ghi Nợ) hoặc phiếu chi đã ghi nhận (ghi Có), kèm số dư luỹ kế.</summary>
public sealed record ProviderTxnDto(
    DateTimeOffset Date, string Type, string RefCode, string? Description,
    decimal Debit, decimal Credit, decimal RunningRemaining);

/// <summary>Tổng hợp sổ cái công nợ 1 NCC: tổng chi phí, đã trả (đã ghi nhận), còn lại.</summary>
public sealed record ProviderTxnSummaryDto(decimal TotalCost, decimal TotalPaid, decimal Remaining);

/// <summary>Lịch sử giao dịch (drill-down) của 1 NCC: sổ cái theo thời gian + tổng hợp.</summary>
public sealed record ProviderTxnHistoryDto(
    Guid ProviderId, string ProviderName,
    ProviderTxnSummaryDto Summary, IReadOnlyList<ProviderTxnDto> Transactions);

/// <summary>Tổng quan hoạt động kinh doanh (legacy BusinessActivity/HomePage): doanh thu/thu/chi/công nợ/lợi nhuận.</summary>
public sealed record DashboardSummaryDto(
    int OrderCount,
    decimal TotalRevenue, decimal TotalReceived, decimal ReceivableOutstanding,
    decimal TotalCost, decimal TotalPaid, decimal PayableOutstanding,
    decimal GrossProfit);

/// <summary>Một dòng dòng tiền theo phương thức thanh toán: thu vào, chi ra, ròng.</summary>
public sealed record CashFlowRowDto(string PaymentMethod, decimal Inflow, decimal Outflow, decimal Net);

/// <summary>Một dòng doanh thu–lợi nhuận theo đơn: doanh thu, chi phí (từ OrderCost), lợi nhuận.</summary>
public sealed record TurnoverRowDto(Guid OrderId, string OrderCode, decimal Revenue, decimal Cost, decimal Profit);

/// <summary>Một dòng hoa hồng/lợi nhuận theo nhân viên sales.</summary>
public sealed record CommissionByUserRowDto(
    Guid UserId, decimal Turnover, decimal Cost, decimal Profit, decimal CommissionRate, decimal CommissionAmount);

/// <summary>Một dòng doanh thu/lợi nhuận theo phòng ban (gom đơn theo phòng ban của sales phụ trách).</summary>
public sealed record TurnoverByDepartmentRowDto(
    Guid? DepartmentId, string DepartmentName, int OrderCount, decimal Turnover, decimal Cost, decimal Profit);

/// <summary>Hiệu suất theo CHI NHÁNH (gom đơn theo Order.BranchId): số đơn · doanh thu · thực thu · còn thiếu · lợi nhuận.</summary>
public sealed record TurnoverByBranchRowDto(
    Guid? BranchId, string BranchName, int OrderCount,
    decimal Turnover, decimal Received, decimal Outstanding, decimal Cost, decimal Profit);

/// <summary>Top khách hàng theo doanh thu (gom đơn theo Customer): tổng doanh thu + đã thu.</summary>
public sealed record TopCustomerRowDto(
    Guid CustomerId, string CustomerName, decimal Revenue, decimal Received);

/// <summary>
/// KPI phễu kinh doanh (legacy KeyPerformanceIndicator): báo giá → chấp nhận → chuyển đơn → thu tiền.
/// Các tỉ lệ là phân số 0..1 (FE hiển thị %). Tính từ dữ liệu sẵn có, không phụ thuộc ngoài.
/// </summary>
public sealed record KpiSummaryDto(
    int QuoteCount, int QuoteAcceptedCount, int QuoteConvertedCount,
    decimal AcceptanceRate, decimal ConversionRate,
    int OrderCount, decimal TotalRevenue, decimal AvgOrderValue,
    decimal TotalReceived, decimal CollectionRate);
