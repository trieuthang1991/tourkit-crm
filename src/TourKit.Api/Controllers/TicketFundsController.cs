using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Quỹ vé ứng (legacy TicketFund) dưới /api/v1/ticket-funds.</summary>
[ApiController]
[Route("api/v1/ticket-funds")]
public sealed class TicketFundsController(ITicketFundService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] TicketFundListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.TicketFundView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Create([FromBody] CreateTicketFundDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/ticket-funds/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.TicketFundManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketFundDto dto)
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
