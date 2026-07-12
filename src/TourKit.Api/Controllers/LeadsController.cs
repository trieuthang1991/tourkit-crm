using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/leads")]
public sealed class LeadsController(ILeadService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.LeadView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] LeadListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    // Thẻ thống kê đầu màn Lead: tổng + đếm theo trạng thái + đã chuyển KH.
    [HttpGet("stats")]
    [Authorize(Permissions.LeadView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.LeadView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var lead = await service.GetAsync(id);
        return Ok(lead);
    }

    [HttpPost]
    [Authorize(Permissions.LeadCreate)]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.LeadUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.LeadDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert")]
    [Authorize(Permissions.LeadConvert)]
    public async Task<IActionResult> Convert(Guid id)
    {
        var result = await service.ConvertAsync(id);
        return Created($"/api/v1/customers/{result.CustomerId}", result);
    }
}
