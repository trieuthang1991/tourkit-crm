using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Admin;

namespace TourKit.Api.Controllers;

/// <summary>
/// Quản lý vai trò + gán quyền dưới /api/v1/roles. Không có quyền RBAC riêng trong catalog nên
/// tái dùng user.view (đọc) / user.manage (ghi) — cùng nhóm quản trị user.
/// </summary>
[ApiController]
[Route("api/v1/roles")]
public sealed class RolesController(IRoleAdminService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListRolesAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetRoleAsync(id));

    [HttpPost]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/roles/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest dto)
        => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
