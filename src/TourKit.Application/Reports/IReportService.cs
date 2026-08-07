using TourKit.Application.Reports.Dtos;

namespace TourKit.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync();
    Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync();
    Task<ProviderTxnHistoryDto> GetProviderTransactionsAsync(Guid providerId);
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync();
    Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync();
    Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync();
    Task<IReadOnlyList<CommissionByMilestoneRowDto>> GetCommissionByMilestoneAsync(DateTimeOffset? from, DateTimeOffset? to);
    Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync();
    Task<IReadOnlyList<MoneyByTourTypeRowDto>> GetMoneyByTourTypeAsync();
    Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync();
    Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10);
    Task<KpiSummaryDto> GetKpiSummaryAsync();

    /// <summary>Nhịp doanh thu cho màn Bàn làm việc (chuỗi theo ngày + mốc cộng dồn + kỳ trước).</summary>
    Task<WorkspacePulseDto> GetWorkspacePulseAsync(int days = 7);
}
