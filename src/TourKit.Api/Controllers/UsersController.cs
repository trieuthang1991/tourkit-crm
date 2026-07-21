using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Application.Auth;
using TourKit.Api.Authz;
using TourKit.Application.Admin;

namespace TourKit.Api.Controllers;

/// <summary>
/// Quản lý user trong tenant dưới /api/v1/users — liệt kê, tạo/sửa/khoá, đặt lại mật khẩu,
/// gán phòng ban/chức vụ/vai trò. Mật khẩu được hash tại controller (IPasswordHasher ở tầng Api)
/// rồi truyền xuống service. Đọc dùng quyền user.view, ghi dùng user.manage.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserAdminService service, IPasswordHasher hasher) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Password))
        {
            return ValidationProblem("Mật khẩu không được trống.");
        }

        var data = new CreateUserData(
            req.Email, req.FullName, hasher.Hash(req.Password),
            req.DepartmentId, req.PositionId, req.RoleId, req.IsActive);
        var created = await service.CreateAsync(data);
        return Created($"/api/v1/users/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
        => Ok(await service.UpdateAsync(id, req));

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
        {
            return ValidationProblem("Mật khẩu không được trống.");
        }

        await service.ResetPasswordAsync(id, hasher.Hash(req.NewPassword));
        return NoContent();
    }

    [HttpPost("{id:guid}/toggle-active")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> ToggleActive(Guid id)
        => Ok(await service.ToggleActiveAsync(id));

    [HttpPut("{id:guid}/org")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> AssignOrg(Guid id, [FromBody] AssignUserOrgDto dto)
        => Ok(await service.AssignOrgAsync(id, dto));
}
