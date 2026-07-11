using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Chức vụ (legacy Position) dưới /api/v1/positions — dùng quyền quản lý user.</summary>
[ApiController]
[Route("api/v1/positions")]
public sealed class PositionsController(IPositionService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreatePositionDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/positions/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.UserManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePositionDto dto)
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
