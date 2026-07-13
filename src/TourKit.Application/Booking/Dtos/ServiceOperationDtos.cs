namespace TourKit.Application.Booking.Dtos;

/// <summary>Phiếu điều hành dịch vụ (legacy "Phiếu điều hành dịch vụ") — 1 dòng = 1 ServiceBooking, theo dõi chi NCC.</summary>
public sealed record ServiceOperationDto(
    Guid Id, string Code, string? ProviderName, string Description, DateTimeOffset? UsageDate,
    decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, int PaymentStatus);

/// <summary>Bộ lọc: Mã phiếu/NCC/tên DV · NCC · trạng thái chi (0 chờ chi, 1 chưa chi hết, 2 thành công).</summary>
public sealed record ServiceOperationListFilter(string? Q = null, Guid? ProviderId = null, int? PaymentStatus = null);

/// <summary>Thẻ đầu màn + footer: tổng · chưa TT(chờ chi) · đặt cọc(chưa chi hết) · hoàn thành + tổng chi/đã TT/còn thiếu.</summary>
public sealed record ServiceOperationStatsDto(
    int Total, int Unpaid, int Partial, int Done,
    decimal TotalCost, decimal TotalPaid, decimal TotalRemaining);

/// <summary>Ghi nhận thanh toán NCC cho phiếu (cập nhật số đã thanh toán).</summary>
public sealed record PayServiceOperationDto(decimal PaidAmount);
