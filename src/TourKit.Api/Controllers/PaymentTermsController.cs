using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Điều khoản thanh toán NCC (legacy ServicePaymentTerm) dưới /api/v1/payment-terms.</summary>
[ApiController]
[Route("api/v1/payment-terms")]
public sealed class PaymentTermsController(IPaymentTermService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ProviderView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.ProviderCreate)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentTermDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/payment-terms/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ProviderCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentTermDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ProviderCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
