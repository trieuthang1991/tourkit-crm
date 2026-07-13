using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Phiếu điều hành dịch vụ (legacy "Phiếu điều hành dịch vụ") dưới /api/v1/service-operations — theo dõi chi NCC.</summary>
[ApiController]
[Route("api/v1/service-operations")]
public sealed class ServiceOperationsController(IServiceOperationService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceBookingView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] ServiceOperationListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.ServiceBookingView)]
    public async Task<IActionResult> Stats([FromQuery] ServiceOperationListFilter? filter = null)
        => Ok(await service.GetStatsAsync(filter));

    [HttpPost("{id:guid}/pay")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayServiceOperationDto dto)
        => Ok(await service.PayAsync(id, dto));
}
