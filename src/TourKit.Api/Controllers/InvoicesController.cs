using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Hoá đơn VAT dưới /api/v1/invoices: CRUD header + dòng, subtotal/VAT/total tự tính.</summary>
[ApiController]
[Route("api/v1/invoices")]
public sealed class InvoicesController(IInvoiceService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.InvoiceView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.InvoiceView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var invoice = await service.GetAsync(id);
        return Ok(invoice);
    }

    [HttpPost]
    [Authorize(Permissions.InvoiceManage)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/invoices/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.InvoiceManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDto dto)
    {
        var updated = await service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.InvoiceManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
