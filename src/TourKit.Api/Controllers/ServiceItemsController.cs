using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/service-items")]
public sealed class ServiceItemsController(IServiceItemService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
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
    public async Task<IActionResult> Create([FromBody] CreateServiceItemDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceItemDto dto)
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
