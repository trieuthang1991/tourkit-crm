using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Tài khoản nhận tiền (legacy PaymentMethod) dưới /api/v1/payment-accounts — in lên báo giá/hoá đơn.</summary>
[ApiController]
[Route("api/v1/payment-accounts")]
public sealed class PaymentAccountsController(IPaymentAccountService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.PaymentAccountView)]
    public async Task<IActionResult> List()
    {
        var items = await service.ListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Permissions.PaymentAccountManage)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentAccountDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/payment-accounts/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.PaymentAccountManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentAccountDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.PaymentAccountManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
