using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/commission-rules")]
public sealed class CommissionRulesController(ICommissionRuleService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] CommissionRuleListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCommissionRuleDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/commission-rules/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommissionRuleDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
