using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Yêu cầu báo giá của Đại lý (B2B MVP §4.2.3 — module trọng tâm) dưới /api/v1/agent-quotes.
/// Workflow: create (Requested) → quote (Quoted) → confirm (Confirmed) | reject (Rejected).
/// </summary>
[ApiController]
[Route("api/v1/agent-quotes")]
public sealed class AgentQuoteRequestsController(IAgentQuoteRequestService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] AgentQuoteRequestListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.AgentQuoteView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> Create([FromBody] CreateAgentQuoteRequestDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/agent-quotes/{created.Id}", created);
    }

    [HttpPost("{id:guid}/quote")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> Quote(Guid id, [FromBody] QuoteAgentRequestDto dto)
        => Ok(await service.QuoteAsync(id, dto));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> Confirm(Guid id) => Ok(await service.ConfirmAsync(id));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Permissions.AgentQuoteManage)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAgentQuoteBody? body)
        => Ok(await service.RejectAsync(id, body?.Note));
}

/// <summary>Body tuỳ chọn cho reject (ghi chú lý do).</summary>
public sealed record RejectAgentQuoteBody(string? Note);
