using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/tour-templates")]
public sealed class TourTemplatesController(ITourTemplateService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TourView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.TourView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var template = await service.GetAsync(id);
        return Ok(template);
    }

    [HttpPost]
    [Authorize(Permissions.TourCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTourTemplateDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.TourUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTourTemplateDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.TourDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/itinerary")]
    [Authorize(Permissions.TourView)]
    public async Task<IActionResult> GetItinerary(Guid id)
    {
        var days = await service.GetItineraryAsync(id);
        return Ok(days);
    }

    [HttpPut("{id:guid}/itinerary")]
    [Authorize(Permissions.TourUpdate)]
    public async Task<IActionResult> ReplaceItinerary(Guid id, [FromBody] ItineraryDayDto[] days)
    {
        await service.ReplaceItineraryAsync(id, days);
        return NoContent();
    }

    [HttpGet("{id:guid}/price-scenarios")]
    [Authorize(Permissions.TourView)]
    public async Task<IActionResult> GetPriceScenarios(Guid id)
    {
        var scenarios = await service.GetPriceScenariosAsync(id);
        return Ok(scenarios);
    }

    [HttpPut("{id:guid}/price-scenarios")]
    [Authorize(Permissions.TourUpdate)]
    public async Task<IActionResult> ReplacePriceScenarios(Guid id, [FromBody] PriceScenarioDto[] scenarios)
    {
        await service.ReplacePriceScenariosAsync(id, scenarios);
        return NoContent();
    }
}
