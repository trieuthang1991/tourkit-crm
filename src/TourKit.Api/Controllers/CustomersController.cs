using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20,
        [FromQuery] CustomerListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    // Thẻ thống kê đầu màn (bám hệ cũ): tổng KH / tạo hôm nay / tháng này / mua lần đầu / mua lại.
    [HttpGet("stats")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    // Giá trị có sẵn cho dropdown lọc (nguồn, tag, chi nhánh, chiến dịch...) — user chọn thay vì gõ tay.
    [HttpGet("filter-options")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> FilterOptions() => Ok(await service.GetFilterOptionsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var customer = await service.GetAsync(id);
        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Permissions.CustomerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CustomerDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
