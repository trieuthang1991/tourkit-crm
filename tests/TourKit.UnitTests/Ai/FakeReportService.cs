using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Bản giả IReportService cho test công cụ. Hàm chưa cài ném NotImplementedException CÓ CHỦ Ý: một
/// công cụ gọi nhầm hàm thì test đỏ ngay, thay vì âm thầm trả danh sách rỗng rồi trợ lý trả lời
/// "không có dữ liệu" trong khi dữ liệu có thật.
/// </summary>
internal sealed class FakeReportService : IReportService
{
    public IReadOnlyList<OrderDebtRowDto> Debts { get; init; } = [];

    public IReadOnlyList<TopCustomerRowDto> TopCustomers { get; init; } = [];

    public DashboardSummaryDto? Dashboard { get; init; }

    /// <summary>Số lượng top mà công cụ thực sự yêu cầu — để kiểm tra việc kẹp tham số.</summary>
    public int LastTopRequested { get; private set; }

    public Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync() => Task.FromResult(Debts);

    public Task<DashboardSummaryDto> GetDashboardAsync() => Task.FromResult(Dashboard!);

    public Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10)
    {
        LastTopRequested = top;
        return Task.FromResult<IReadOnlyList<TopCustomerRowDto>>([.. TopCustomers.Take(top)]);
    }

    public Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync() => throw new NotImplementedException();

    public Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync() => throw new NotImplementedException();

    public Task<ProviderTxnHistoryDto> GetProviderTransactionsAsync(Guid providerId) => throw new NotImplementedException();

    public Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync() => throw new NotImplementedException();

    public Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync() => throw new NotImplementedException();

    public Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync() => throw new NotImplementedException();

    public Task<IReadOnlyList<CommissionByMilestoneRowDto>> GetCommissionByMilestoneAsync(DateTimeOffset? from, DateTimeOffset? to)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync() => throw new NotImplementedException();

    public Task<IReadOnlyList<MoneyByTourTypeRowDto>> GetMoneyByTourTypeAsync() => throw new NotImplementedException();

    public Task<KpiSummaryDto> GetKpiSummaryAsync() => throw new NotImplementedException();

    public Task<WorkspacePulseDto> GetWorkspacePulseAsync(int days = 7) => throw new NotImplementedException();

    public Task<IReadOnlyList<RevenuePointDto>> GetRevenueSeriesAsync(DateTimeOffset from, DateTimeOffset to, bool monthly)
        => throw new NotImplementedException();
}
