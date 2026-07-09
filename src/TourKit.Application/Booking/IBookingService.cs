using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IBookingService
{
    /// <summary>Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.</summary>
    Task<OrderDto> CreateBookingAsync(Guid departureId, CreateBookingDto dto);

    /// <summary>Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).</summary>
    Task<SeatDto> CreateHoldAsync(Guid departureId, CreateBookingDto dto);

    /// <summary>Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".</summary>
    Task<SeatDto> ConfirmSeatAsync(Guid seatId);

    /// <summary>Đặt cọc: cộng vào upfront_amount của chỗ.</summary>
    Task<SeatDto> DepositAsync(Guid seatId, DepositDto dto);

    /// <summary>Huỷ chỗ + hoàn tiền (legacy CancelSeats + statusCancel != 0).</summary>
    Task<SeatDto> CancelSeatAsync(Guid seatId, CancelSeatDto dto);

    Task<SeatDto> GetSeatAsync(Guid seatId);

    Task<PagedResult<OrderDto>> ListOrdersAsync(int page, int size);

    Task<IReadOnlyList<BookingLineDto>> ListOrderLinesAsync(Guid orderId);

    /// <summary>Gán (hoặc gỡ, khi SalesUserId = null) nhân viên sales phụ trách đơn.</summary>
    Task<OrderDto> AssignSalesAsync(Guid orderId, AssignSalesDto dto);
}
