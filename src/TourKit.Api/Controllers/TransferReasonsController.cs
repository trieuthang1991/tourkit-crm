using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục lý do chuyển chuyến (legacy ReasonSwitch) dưới /api/v1/transfer-reasons — dùng quyền booking.</summary>
[ApiController]
[Route("api/v1/transfer-reasons")]
public sealed class TransferReasonsController(ITransferReasonService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTransferReasonDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/transfer-reasons/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransferReasonDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
