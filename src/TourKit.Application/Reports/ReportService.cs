using TourKit.Application.Reports.Dtos;

namespace TourKit.Application.Reports;

/// <summary>Báo cáo (read-only) — mỏng, forward sang <see cref="IReportQueries"/> (query GROUP BY nhiều bảng).</summary>
public sealed class ReportService(IReportQueries queries) : IReportService
{
    public Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync() => queries.GetOrderDebtAsync();

    public Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync() => queries.GetProviderDebtAsync();

    public Task<DashboardSummaryDto> GetDashboardAsync() => queries.GetDashboardAsync();

    public Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync() => queries.GetCashFlowAsync();

    public Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync() => queries.GetTurnoverAsync();

    public Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync() => queries.GetCommissionByUserAsync();

    public Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync() => queries.GetTurnoverByDepartmentAsync();

    public Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync() => queries.GetTurnoverByBranchAsync();

    public Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10) => queries.GetTopCustomersAsync(top);

    public Task<KpiSummaryDto> GetKpiSummaryAsync() => queries.GetKpiSummaryAsync();
}
