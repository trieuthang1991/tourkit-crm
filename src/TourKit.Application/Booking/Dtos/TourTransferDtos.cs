namespace TourKit.Application.Booking.Dtos;

public sealed record TourTransferDto(
    Guid Id, Guid OrderId, Guid FromDepartureId, Guid ToDepartureId,
    string? Reason, Guid? ReasonId, string? ReasonName, DateTimeOffset TransferredAt);

/// <summary>Yêu cầu chuyển đơn sang chuyến khác (đổi lịch, giữ nguyên giá). ReasonId = lý do chuẩn (tuỳ chọn).</summary>
public sealed record TransferOrderDto(Guid ToDepartureId, string? Reason, Guid? ReasonId);
