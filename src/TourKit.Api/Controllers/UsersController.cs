using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Admin;

namespace TourKit.Api.Controllers;

/// <summary>Quản lý user trong tenant dưới /api/v1/users — liệt kê + gán phòng ban/chức vụ.</summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserAdminService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPut("{id:guid}/org")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> AssignOrg(Guid id, [FromBody] AssignUserOrgDto dto)
        => Ok(await service.AssignOrgAsync(id, dto));
}
