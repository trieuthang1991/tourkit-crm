using TourKit.Application.Ai;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;
using TourKit.Application.Collaboration;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Common;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Bản giả các service mà <c>AiRecordSheet</c> dùng. Hàm không dùng tới ném NotImplementedException
/// CÓ CHỦ Ý: dựng hồ sơ mà gọi nhầm hàm thì test đỏ ngay, thay vì âm thầm nhận về dữ liệu rỗng rồi
/// AI trả lời "không có thông tin" trong khi dữ liệu có thật.
/// </summary>
internal sealed class FakeLeadService(LeadDto? lead) : ILeadService
{
    public Task<LeadDto> GetAsync(Guid id) =>
        lead is null ? throw new InvalidOperationException("Test không dựng sẵn cơ hội.") : Task.FromResult(lead);

    /// <summary>Không có bản ghi tiền thân trong các test dựng hồ sơ AI — trả null là đúng ngữ cảnh.</summary>
    public Task<LeadDto?> FindByConvertedCustomerAsync(Guid customerId) => Task.FromResult<LeadDto?>(null);

    public Task<PagedResult<LeadDto>> ListAsync(int page, int size, LeadListFilter? filter = null) => throw new NotImplementedException();

    public Task<LeadStatsDto> GetStatsAsync() => throw new NotImplementedException();

    public Task<LeadFilterOptionsDto> GetFilterOptionsAsync() => throw new NotImplementedException();

    public Task<LeadDto> CreateAsync(CreateLeadDto dto) => throw new NotImplementedException();

    public Task UpdateAsync(Guid id, UpdateLeadDto dto) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task<ConvertLeadResultDto> ConvertAsync(Guid id, Guid? assignedToUserId = null) => throw new NotImplementedException();
}

internal sealed class FakeCustomerService(CustomerDto? customer) : ICustomerService
{
    public Task<CustomerDto> GetAsync(Guid id) =>
        customer is null ? throw new InvalidOperationException("Test không dựng sẵn khách hàng.") : Task.FromResult(customer);

    public Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CustomerListFilter? filter = null) => throw new NotImplementedException();

    public Task<CustomerStatsDto> GetStatsAsync() => throw new NotImplementedException();

    public Task<CustomerFilterOptionsDto> GetFilterOptionsAsync() => throw new NotImplementedException();

    public Task<CustomerFunnelDto> GetFunnelAsync() => throw new NotImplementedException();

    public Task<CustomerDto> CreateAsync(CreateCustomerDto dto) => throw new NotImplementedException();

    public Task UpdateAsync(Guid id, UpdateCustomerDto dto) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task<IReadOnlyList<DuplicateGroupDto>> FindDuplicatesAsync() => throw new NotImplementedException();

    public Task<DuplicateCustomerDto?> FindByPhoneAsync(string? phone, Guid? excludeId = null) => throw new NotImplementedException();
}

internal sealed class FakeCommentService(IReadOnlyList<EntityCommentDto> comments) : IEntityCommentService
{
    /// <summary>Số dòng mà nơi gọi thực sự yêu cầu — để kiểm tra biên được truyền xuống chứ không cắt ở bộ nhớ.</summary>
    public int LastTake { get; private set; }

    public Task<IReadOnlyList<EntityCommentDto>> ListAsync(string entityName, string entityId, int take = 50)
    {
        LastTake = take;
        return Task.FromResult<IReadOnlyList<EntityCommentDto>>([.. comments.Take(take)]);
    }

    public Task<int> CountAsync(string entityName, string entityId) => Task.FromResult(comments.Count);

    public Task<EntityCommentDto> CreateAsync(CreateEntityCommentDto dto) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}

/// <summary>
/// Cơ hội bán hàng cho hồ sơ AI. Chỉ dựng <c>GetAsync</c> và danh mục bước phễu — hai thứ
/// <c>AiRecordSheet</c> thật sự đọc; còn lại ném để gọi nhầm là đỏ ngay.
/// </summary>
internal sealed class FakeOpportunityService(SalesOpportunityDto? opp, params OpportunityStageDto[] stages)
    : ISalesOpportunityService
{
    public Task<SalesOpportunityDto> GetAsync(Guid id) =>
        opp is null ? throw new InvalidOperationException("Test không dựng sẵn cơ hội.") : Task.FromResult(opp);

    public Task<IReadOnlyList<OpportunityStageDto>> ListStagesAsync() =>
        Task.FromResult<IReadOnlyList<OpportunityStageDto>>([.. stages]);

    public Task<PagedResult<SalesOpportunityDto>> ListAsync(int page, int size, SalesOpportunityListFilter? filter = null) => throw new NotImplementedException();

    public Task<SalesOpportunityStatsDto> GetStatsAsync(SalesOpportunityListFilter? filter = null) => throw new NotImplementedException();

    public Task<SalesOpportunityDto> CreateAsync(CreateSalesOpportunityDto dto) => throw new NotImplementedException();

    public Task<SalesOpportunityDto> UpdateAsync(Guid id, UpdateSalesOpportunityDto dto) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task<SalesOpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto) => throw new NotImplementedException();

    public Task<IReadOnlyList<OpportunityByUserRowDto>> ReportByUserAsync(DateTimeOffset? tu, DateTimeOffset? den) => throw new NotImplementedException();

    public Task<IReadOnlyList<OpportunityCancelReasonRowDto>> ReportCancelReasonsAsync(DateTimeOffset? tu, DateTimeOffset? den) => throw new NotImplementedException();
}

/// <summary>Kho kết quả AI đã lưu. Trả sẵn danh sách dựng trong test, không ghi đi đâu.</summary>
internal sealed class FakeInsightStore(params AiInsightDto[] items) : IAiInsightStore
{
    public Task<IReadOnlyList<AiInsightDto>> LatestAsync(string entityName, string entityId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AiInsightDto>>([.. items]);

    public Task<Guid> SaveAsync(SaveAiInsightDto dto, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<IReadOnlyList<AiInsightDto>> HistoryAsync(string entityName, string entityId, string kind, int take = 20, CancellationToken ct = default) => throw new NotImplementedException();
}
