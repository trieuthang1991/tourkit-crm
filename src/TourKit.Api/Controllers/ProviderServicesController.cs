using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/provider-services")]
public sealed class ProviderServicesController(IProviderServiceService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] Guid? providerId = null)
    {
        var result = await service.ListAsync(page, size, providerId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.ServiceView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await service.GetAsync(id);
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Create([FromBody] CreateProviderServiceDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderServiceDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
