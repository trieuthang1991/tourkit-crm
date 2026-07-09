using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/market-types")]
public sealed class MarketTypesController(IMarketTypeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.MarketView)]
    public async Task<IActionResult> List()
    {
        var items = await service.ListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Permissions.MarketManage)]
    public async Task<IActionResult> Create([FromBody] CreateMarketTypeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/market-types/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.MarketManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMarketTypeDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.MarketManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
