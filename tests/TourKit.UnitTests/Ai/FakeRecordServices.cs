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

    public Task<PagedResult<LeadDto>> ListAsync(int page, int size, LeadListFilter? filter = null) => throw new NotImplementedException();

    public Task<LeadStatsDto> GetStatsAsync() => throw new NotImplementedException();

    public Task<LeadFilterOptionsDto> GetFilterOptionsAsync() => throw new NotImplementedException();

    public Task<LeadDto> CreateAsync(CreateLeadDto dto) => throw new NotImplementedException();

    public Task UpdateAsync(Guid id, UpdateLeadDto dto) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task<ConvertLeadResultDto> ConvertAsync(Guid id) => throw new NotImplementedException();
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
