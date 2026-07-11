using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục loại khách (legacy customer_type) dưới /api/v1/customer-types.</summary>
[ApiController]
[Route("api/v1/customer-types")]
public sealed class CustomerTypesController(ICustomerTypeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CustomerTypeView)]
    public async Task<IActionResult> List()
    {
        var items = await service.ListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Permissions.CustomerTypeManage)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerTypeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/customer-types/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CustomerTypeManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerTypeDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CustomerTypeManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
