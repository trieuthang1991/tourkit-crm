using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Tỷ giá tiền tệ (legacy ExchangeRate) dưới /api/v1/currencies — dùng quyền quản lý NCC/dịch vụ.</summary>
[ApiController]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(ICurrencyService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/currencies/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCurrencyDto dto)
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
