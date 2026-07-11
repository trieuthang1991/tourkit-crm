namespace TourKit.Application.Reports.Dtos;

/// <summary>Một dòng công nợ đơn hàng: tổng phải thu, đã thu (phiếu đã duyệt), còn nợ.</summary>
public sealed record OrderDebtRowDto(
    Guid OrderId, string OrderCode, Guid CustomerId, decimal Total, decimal Paid, decimal Outstanding);

/// <summary>Một dòng công nợ nhà cung cấp: tổng chi phí, đã chi (phiếu đã duyệt), còn phải trả.</summary>
public sealed record ProviderDebtRowDto(
    Guid ProviderId, string ProviderName, decimal TotalCost, decimal Paid, decimal Outstanding);

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

/// <summary>
/// KPI phễu kinh doanh (legacy KeyPerformanceIndicator): báo giá → chấp nhận → chuyển đơn → thu tiền.
/// Các tỉ lệ là phân số 0..1 (FE hiển thị %). Tính từ dữ liệu sẵn có, không phụ thuộc ngoài.
/// </summary>
public sealed record KpiSummaryDto(
    int QuoteCount, int QuoteAcceptedCount, int QuoteConvertedCount,
    decimal AcceptanceRate, decimal ConversionRate,
    int OrderCount, decimal TotalRevenue, decimal AvgOrderValue,
    decimal TotalReceived, decimal CollectionRate);
