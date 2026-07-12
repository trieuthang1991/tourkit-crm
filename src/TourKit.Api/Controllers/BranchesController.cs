using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Chi nhánh (legacy ChiNhanh) dưới /api/v1/branches — dùng quyền quản lý user.</summary>
[ApiController]
[Route("api/v1/branches")]
public sealed class BranchesController(IBranchService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreateBranchDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/branches/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchDto dto)
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
