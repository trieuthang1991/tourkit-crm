using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Customers;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
        => Ok(await service.ListAsync(page, size, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Permissions.CustomerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CustomerDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
