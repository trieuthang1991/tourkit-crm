using TourKit.Application.Reports.Dtos;

namespace TourKit.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync();
    Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync();
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync();
    Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync();
    Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync();
    Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync();
    Task<KpiSummaryDto> GetKpiSummaryAsync();
}
