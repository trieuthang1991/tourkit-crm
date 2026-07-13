using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/customer-cares")]
public sealed class CustomerCaresController(ICustomerCareService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CareView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] CustomerCareListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.CareView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CareView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var care = await service.GetAsync(id);
        return Ok(care);
    }

    [HttpPost]
    [Authorize(Permissions.CareManage)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCareDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CareManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCareDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CareManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
