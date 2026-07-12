using TourKit.Application.Reports.Dtos;

namespace TourKit.Application.Reports;

/// <summary>
/// Repo riêng cho query phức tạp/nhiều bảng (convention §5 — bảng "Query phức tạp/nhiều bảng"): GROUP BY/SUM/COUNT
/// trên Orders/OrderCosts/ReceiptVouchers/PaymentVouchers/Providers/CommissionRules. <c>IRepository&lt;T&gt;</c>
/// generic không đủ cho các phép gộp này. Impl ở Infrastructure (dùng <c>AppDbContext</c> trực tiếp).
/// </summary>
public interface IReportQueries
{
    Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync();
    Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync();
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync();
    Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync();
    Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync();
    Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync();
    Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync();
    Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10);
    Task<KpiSummaryDto> GetKpiSummaryAsync();
}
