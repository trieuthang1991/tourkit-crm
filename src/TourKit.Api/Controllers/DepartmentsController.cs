using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Phòng ban (legacy PhongBan) dưới /api/v1/departments — dùng quyền quản lý user.</summary>
[ApiController]
[Route("api/v1/departments")]
public sealed class DepartmentsController(IDepartmentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/departments/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
