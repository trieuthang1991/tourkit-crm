using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Mở/liệt kê/xem/đóng chuyến khởi hành (TourDeparture) dưới /api/v1/tour-departures.</summary>
[ApiController]
[Route("api/v1/tour-departures")]
public sealed class DeparturesController(IDepartureService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.DepartureView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.DepartureView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var departure = await service.GetAsync(id);
        return Ok(departure);
    }

    [HttpPost]
    [Authorize(Permissions.DepartureCreate)]
    public async Task<IActionResult> Create([FromBody] CreateDepartureDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Permissions.DepartureClose)]
    public async Task<IActionResult> Close(Guid id)
    {
        var closed = await service.CloseAsync(id);
        return Ok(closed);
    }
}
