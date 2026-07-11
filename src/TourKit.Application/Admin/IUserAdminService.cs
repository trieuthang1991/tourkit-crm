namespace TourKit.Application.Admin;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListDto>> ListAsync();
    Task<UserListDto> AssignOrgAsync(Guid userId, AssignUserOrgDto dto);
}
