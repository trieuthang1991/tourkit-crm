namespace TourKit.Application.Booking.Dtos;

public sealed record TourTransferDto(
    Guid Id, Guid OrderId, Guid FromDepartureId, Guid ToDepartureId, string? Reason, DateTimeOffset TransferredAt);

/// <summary>Yêu cầu chuyển đơn sang chuyến khác (đổi lịch, giữ nguyên giá).</summary>
public sealed record TransferOrderDto(Guid ToDepartureId, string? Reason);
