using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IBookingService
{
    /// <summary>Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.
    /// priceOverride: giá chỗ tường minh cho chuyến FIT không template (mặc định lấy giá mẫu tour).</summary>
    Task<OrderDto> CreateBookingAsync(Guid departureId, CreateBookingDto dto, SeatPrices? priceOverride = null);

    /// <summary>Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).</summary>
    Task<SeatDto> CreateHoldAsync(Guid departureId, CreateBookingDto dto);

    /// <summary>Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".</summary>
    Task<SeatDto> ConfirmSeatAsync(Guid seatId);

    /// <summary>Đặt cọc: cộng vào upfront_amount của chỗ.</summary>
    Task<SeatDto> DepositAsync(Guid seatId, DepositDto dto);

    /// <summary>Huỷ chỗ + hoàn tiền (legacy CancelSeats + statusCancel != 0).</summary>
    Task<SeatDto> CancelSeatAsync(Guid seatId, CancelSeatDto dto);

    Task<SeatDto> GetSeatAsync(Guid seatId);

    Task<PagedResult<OrderDto>> ListOrdersAsync(int page, int size, OrderListFilter? filter = null);
    Task<OrderStatsDto> GetOrderStatsAsync();
    Task<OrderFilterOptionsDto> GetOrderFilterOptionsAsync();

    /// <summary>Lấy 1 đơn (đã làm giàu: tên KH/tour, đã thu/còn nợ, pax) — cho màn chi tiết đơn (không tải cả danh sách).</summary>
    Task<OrderDto> GetOrderAsync(Guid orderId);

    Task<IReadOnlyList<BookingLineDto>> ListOrderLinesAsync(Guid orderId);

    /// <summary>Gán (hoặc gỡ, khi SalesUserId = null) nhân viên sales phụ trách đơn.</summary>
    Task<OrderDto> AssignSalesAsync(Guid orderId, AssignSalesDto dto);

    /// <summary>
    /// Tất toán/chốt đơn (legacy ChotDon): gate tuần tự — đơn Confirmed + đã ghi nhận dòng tiền + hoa hồng đã
    /// quyết → Status=Closed + audit. Vi phạm điều kiện ném ValidationAppException.
    /// </summary>
    Task<OrderDto> ConfirmOrderAsync(Guid orderId);
    Task<OrderDto> CancelOrderAsync(Guid orderId);
    Task<OrderDto> CloseOrderAsync(Guid orderId, Guid userId);

    /// <summary>Đổi tình trạng vận hành đơn tour ngay trên dòng (OrderOperationalStatus) — bám staging.</summary>
    Task<OrderDto> SetOperationalStatusAsync(Guid orderId, int status);
    /// <summary>Đổi trạng thái quy trình visa ngay trên dòng (11 bước staging) — VisaStatus 0..10.</summary>
    Task<OrderDto> SetVisaStatusAsync(Guid orderId, int status);

    /// <summary>Mở lại đơn đã tất toán (sửa sai): Closed → Confirmed, xoá audit chốt.</summary>
    Task<OrderDto> ReopenOrderAsync(Guid orderId);
}
