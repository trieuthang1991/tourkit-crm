using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục loại phụ thu (legacy ConfigSurcharge) dưới /api/v1/surcharges.</summary>
[ApiController]
[Route("api/v1/surcharges")]
public sealed class SurchargesController(ISurchargeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Create([FromBody] CreateSurchargeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/surcharges/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSurchargeDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
