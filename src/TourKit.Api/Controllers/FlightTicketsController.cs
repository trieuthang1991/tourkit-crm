using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Vé máy bay đoàn (legacy "Quản lý Vé Đoàn") dưới /api/v1/flight-tickets — quỹ vé theo PNR + gán tour.</summary>
[ApiController]
[Route("api/v1/flight-tickets")]
public sealed class FlightTicketsController(IFlightTicketService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] FlightTicketListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> Stats([FromQuery] FlightTicketListFilter? filter = null)
        => Ok(await service.GetStatsAsync(filter));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Create([FromBody] CreateFlightTicketDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/flight-tickets/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFlightTicketDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignFlightTicketDto dto)
        => Ok(await service.AssignAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
