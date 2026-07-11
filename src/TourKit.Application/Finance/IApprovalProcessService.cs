namespace TourKit.Application.Finance;

/// <summary>Quy trình duyệt cấu hình được (legacy ApprovalProcess): template + bước theo chức vụ + người duyệt.</summary>
public interface IApprovalProcessService
{
    Task<IReadOnlyList<ApprovalProcessDto>> ListAsync();
    Task<ApprovalProcessDetailDto> GetAsync(Guid id);
    Task<ApprovalProcessDto> CreateAsync(CreateApprovalProcessDto dto);
    Task UpdateAsync(Guid id, UpdateApprovalProcessDto dto);
    Task DeleteAsync(Guid id);

    Task<ApprovalProcessStepDto> AddStepAsync(Guid processId, AddStepDto dto);
    Task DeleteStepAsync(Guid processId, Guid stepId);
    Task ReorderStepsAsync(Guid processId, ReorderStepsDto dto);
    Task SetStepUsersAsync(Guid processId, Guid stepId, SetStepUsersDto dto);
}
