using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Chiến dịch chia số Sale (legacy "Chia số Sale") dưới /api/v1/lead-campaigns.</summary>
[ApiController]
[Route("api/v1/lead-campaigns")]
public sealed class LeadCampaignsController(ILeadCampaignService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.LeadView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] LeadCampaignListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.LeadView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.LeadCreate)]
    public async Task<IActionResult> Create([FromBody] CreateLeadCampaignDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/lead-campaigns/{created.Id}", created);
    }
}
