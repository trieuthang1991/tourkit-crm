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
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] DepartureListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.DepartureView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("filter-options")]
    [Authorize(Permissions.DepartureView)]
    public async Task<IActionResult> FilterOptions() => Ok(await service.GetFilterOptionsAsync());

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

    [HttpPost("batch")]
    [Authorize(Permissions.DepartureCreate)]
    public async Task<IActionResult> BatchCreate([FromBody] BatchCreateDeparturesDto dto)
    {
        var result = await service.BatchCreateAsync(dto);
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Permissions.DepartureClose)]
    public async Task<IActionResult> Close(Guid id)
    {
        var closed = await service.CloseAsync(id);
        return Ok(closed);
    }

    /// <summary>Chốt sổ hoa hồng chuyến (legacy StatusComission=1) — khoá hoa hồng đã quyết toán.</summary>
    [HttpPost("{id:guid}/close-commission")]
    [Authorize(Permissions.DepartureClose)]
    public async Task<IActionResult> CloseCommission(Guid id)
    {
        var closed = await service.CloseCommissionAsync(id);
        return Ok(closed);
    }

    /// <summary>Mở lại sổ hoa hồng đã chốt để điều chỉnh (legacy StatusComission=0).</summary>
    [HttpPost("{id:guid}/reopen-commission")]
    [Authorize(Permissions.DepartureClose)]
    public async Task<IActionResult> ReopenCommission(Guid id)
    {
        var reopened = await service.ReopenCommissionAsync(id);
        return Ok(reopened);
    }
}
