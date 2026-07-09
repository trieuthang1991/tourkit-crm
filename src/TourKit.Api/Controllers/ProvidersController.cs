using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/providers")]
public sealed class ProvidersController(IProviderService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ProviderView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.ProviderView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var provider = await service.GetAsync(id);
        return Ok(provider);
    }

    [HttpPost]
    [Authorize(Permissions.ProviderCreate)]
    public async Task<IActionResult> Create([FromBody] CreateProviderDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ProviderUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ProviderDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
