using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/marketing/campaigns")]
public sealed class MarketingController(ICampaignService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.MarketingView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] CampaignListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.MarketingView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.MarketingView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var campaign = await service.GetAsync(id);
        return Ok(campaign);
    }

    [HttpPost]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCampaignDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Permissions.MarketingSend)]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendCampaignDto dto)
    {
        var result = await service.SendAsync(id, dto);
        return Ok(result);
    }

    [HttpGet("{id:guid}/logs")]
    [Authorize(Permissions.MarketingView)]
    public async Task<IActionResult> Logs(Guid id)
    {
        var logs = await service.ListLogsAsync(id);
        return Ok(logs);
    }
}
