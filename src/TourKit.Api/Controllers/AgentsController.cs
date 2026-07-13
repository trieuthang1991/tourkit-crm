using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Đại lý B2B dưới /api/v1/agents (B2B Agent Portal §4.2.1).</summary>
[ApiController]
[Route("api/v1/agents")]
public sealed class AgentsController(IAgentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AgentView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] AgentListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.AgentView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.AgentManage)]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/agents/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.AgentManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.AgentManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
