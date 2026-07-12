using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Đặt khách lên chuyến (Order + dòng TourCustomer) + giữ chỗ / xác nhận chỗ / đặt cọc / huỷ chỗ + đơn.
/// Route trải trên nhiều prefix (tour-departures/tour-customers/orders) — dùng route tuyệt đối theo action,
/// giống <see cref="PaymentsController"/> — thay vì tách 3 controller cho cùng 1 nhóm nghiệp vụ "đặt chỗ".
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class BookingController(IBookingService service) : ControllerBase
{
    // Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.
    [HttpPost("tour-departures/{departureId:guid}/bookings")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> CreateBooking(Guid departureId, [FromBody] CreateBookingDto dto)
    {
        var order = await service.CreateBookingAsync(departureId, dto);
        return Created($"/api/v1/orders/{order.Id}", order);
    }

    // Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).
    [HttpPost("tour-departures/{departureId:guid}/holds")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> CreateHold(Guid departureId, [FromBody] CreateBookingDto dto)
    {
        var seat = await service.CreateHoldAsync(departureId, dto);
        return Created($"/api/v1/tour-customers/{seat.Id}", seat);
    }

    // Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".
    [HttpPost("tour-customers/{seatId:guid}/confirm-seat")]
    [Authorize(Permissions.BookingSeatConfirm)]
    public async Task<IActionResult> ConfirmSeat(Guid seatId)
    {
        var seat = await service.ConfirmSeatAsync(seatId);
        return Ok(seat);
    }

    // Đặt cọc: cộng vào upfront_amount của chỗ.
    [HttpPost("tour-customers/{seatId:guid}/deposit")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Deposit(Guid seatId, [FromBody] DepositDto dto)
    {
        var seat = await service.DepositAsync(seatId, dto);
        return Ok(seat);
    }

    // Huỷ chỗ + hoàn tiền (legacy CancelSeats + statusCancel != 0).
    [HttpPost("tour-customers/{seatId:guid}/cancel")]
    [Authorize(Permissions.BookingSeatCancel)]
    public async Task<IActionResult> CancelSeat(Guid seatId, [FromBody] CancelSeatDto dto)
    {
        var seat = await service.CancelSeatAsync(seatId, dto);
        return Ok(seat);
    }

    [HttpGet("tour-customers/{seatId:guid}")]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> GetSeat(Guid seatId)
    {
        var seat = await service.GetSeatAsync(seatId);
        return Ok(seat);
    }

    [HttpGet("orders")]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] OrderListFilter? filter = null)
    {
        var orders = await service.ListOrdersAsync(page, size, filter);
        return Ok(orders);
    }

    // Thẻ thống kê đầu màn Đơn hàng: tổng đơn + doanh thu/đã thu/còn nợ + đếm theo trạng thái.
    [HttpGet("orders/stats")]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> OrderStats() => Ok(await service.GetOrderStatsAsync());

    // Tuỳ chọn lọc động (Loại tour) lấy từ dữ liệu thật.
    [HttpGet("orders/filter-options")]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> OrderFilterOptions() => Ok(await service.GetOrderFilterOptionsAsync());

    [HttpGet("orders/{orderId:guid}/lines")]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> ListOrderLines(Guid orderId)
    {
        var lines = await service.ListOrderLinesAsync(orderId);
        return Ok(lines);
    }

    // Gán sales phụ trách đơn (legacy id_sales_root trên Orders).
    [HttpPut("orders/{orderId:guid}/sales")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> AssignSales(Guid orderId, [FromBody] AssignSalesDto dto)
    {
        var order = await service.AssignSalesAsync(orderId, dto);
        return Ok(order);
    }
}
