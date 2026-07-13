using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Đặt chỗ của Đại lý (B2B §4.2.4/4.2.5) dưới /api/v1/agent-bookings — tạo từ quote Confirmed + hành khách.</summary>
[ApiController]
[Route("api/v1/agent-bookings")]
public sealed class AgentBookingsController(IAgentBookingService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] AgentBookingListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> Create([FromBody] CreateAgentBookingDto dto)
    {
        var created = await service.CreateFromQuoteAsync(dto);
        return Created($"/api/v1/agent-bookings/{created.Id}", created);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAgentBookingStatusBody body)
    {
        await service.UpdateStatusAsync(id, body.Status);
        return NoContent();
    }

    [HttpPost("{id:guid}/passengers")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> AddPassenger(Guid id, [FromBody] AddAgentPassengerDto dto)
        => Ok(await service.AddPassengerAsync(id, dto));

    [HttpDelete("{id:guid}/passengers/{passengerId:guid}")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> RemovePassenger(Guid id, Guid passengerId)
    {
        await service.RemovePassengerAsync(id, passengerId);
        return NoContent();
    }
}

public sealed record UpdateAgentBookingStatusBody(int Status);
