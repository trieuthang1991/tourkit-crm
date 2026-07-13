using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Vé máy bay lẻ (legacy "Quản lý Vé máy bay lẻ") dưới /api/v1/flight-tickets-individual — lưới vận hành P/L từng vé.</summary>
[ApiController]
[Route("api/v1/flight-tickets-individual")]
public sealed class FlightTicketsIndividualController(IFlightTicketIndividualService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] FlightTicketIndividualListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> Stats([FromQuery] FlightTicketIndividualListFilter? filter = null)
        => Ok(await service.GetStatsAsync(filter));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Create([FromBody] CreateFlightTicketIndividualDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/flight-tickets-individual/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFlightTicketIndividualDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
