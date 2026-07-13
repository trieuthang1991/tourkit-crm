using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Quy tắc hoa hồng theo loại khách dưới /api/v1/customer-commission-rules (dùng chung perm commission.*).</summary>
[ApiController]
[Route("api/v1/customer-commission-rules")]
public sealed class CustomerCommissionRulesController(ICustomerCommissionRuleService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] CustomerCommissionRuleListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommissionRuleDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/customer-commission-rules/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommissionRuleDto dto)
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
