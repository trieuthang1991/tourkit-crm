using TourKit.Shared.Enums;

namespace TourKit.Application.Booking.Dtos;

/// <summary>Yêu cầu đặt khách (dùng chung giữa "đặt chốt ngay" và "giữ chỗ").</summary>
public sealed record CreateBookingDto(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

/// <summary>
/// Giá chỗ truyền tường minh (thay vì lấy từ mẫu tour) — dùng cho chuyến RIÊNG FIT không template:
/// giá theo hạng khách lấy từ BÁO GIÁ đã chốt (QuoteMath), không phải giá niêm yết.
/// </summary>
public sealed record SeatPrices(decimal Adult, decimal Child, decimal ChildSmall, decimal Baby);

public sealed record OrderDto(
    Guid Id, string Code, Guid TourDepartureId, Guid CustomerId, decimal TotalRevenue, decimal TotalCost,
    OrderStatus Status, Guid? SalesUserId,
    // Trường làm giàu cho danh sách (tên KH/tour/ngày đi + đã thu/còn nợ). Mặc định null/0 để các
    // đường trả đơn lẻ (tạo booking, gán sales) không phải nạp thêm dữ liệu.
    string? CustomerName = null, string? TourTitle = null, DateTimeOffset? DepartureDate = null,
    decimal AmountPaid = 0m, decimal Outstanding = 0m);

/// <summary>Bộ lọc danh sách đơn hàng (bám thanh lọc hệ cũ). PaymentStatus: 0 chưa TT · 1 đã cọc · 2 TT hết.</summary>
public sealed record OrderListFilter(
    string? Q = null, int? Status = null, int? PaymentStatus = null,
    DateTimeOffset? DepartureFrom = null, DateTimeOffset? DepartureTo = null,
    DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null,
    Guid? SalesUserId = null, Guid? BranchId = null, Guid? CreatedByUserId = null, Guid? DepartmentId = null,
    string? TourType = null, Guid? ProviderId = null);

/// <summary>NCC xuất hiện trong đơn (dùng cho Select lọc theo nhà cung cấp).</summary>
public sealed record OrderFilterProviderDto(Guid Id, string Name);

/// <summary>Tuỳ chọn lọc động cho màn Đơn hàng (lấy từ dữ liệu thật, không hardcode).</summary>
public sealed record OrderFilterOptionsDto(IReadOnlyList<string> TourTypes, IReadOnlyList<OrderFilterProviderDto> Providers);

/// <summary>Thẻ thống kê đầu màn Đơn hàng: tổng đơn + tiền + đếm theo trạng thái + trạng thái thanh toán.</summary>
public sealed record OrderStatsDto(
    int Total, decimal TotalRevenue, decimal TotalPaid, decimal TotalOutstanding,
    int Draft, int Confirmed, int Cancelled,
    int Unpaid, int Deposit, int Paid);

public sealed record BookingLineDto(
    Guid Id, int Quantity, int AmountChildren, int AmountChildrenSmall, int QuantityBaby,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal UpfrontAmount, string? ReservationCode, bool IsMainContact);

public sealed record DepositDto(decimal Amount);

public sealed record CancelSeatDto(string? Note, decimal RefundAmount);

public sealed record SeatDto(
    Guid Id, Guid OrderId, SeatStatus Status, decimal UpfrontAmount, decimal LineTotal,
    DateTimeOffset? HoldExpiresAt, string? ReservationCode);

public sealed record AssignSalesDto(Guid? SalesUserId);
