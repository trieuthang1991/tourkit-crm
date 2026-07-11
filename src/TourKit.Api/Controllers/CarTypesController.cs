using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục loại xe (legacy CarType) dưới /api/v1/car-types — dùng quyền quản lý xe.</summary>
[ApiController]
[Route("api/v1/car-types")]
public sealed class CarTypesController(ICarTypeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.VehicleView)]
    public async Task<IActionResult> List()
    {
        var items = await service.ListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Create([FromBody] CreateCarTypeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/car-types/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCarTypeDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
