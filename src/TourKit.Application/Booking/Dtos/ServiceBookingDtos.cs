using TourKit.Shared.Enums;

namespace TourKit.Application.Booking.Dtos;

public sealed record ServiceBookingDto(
    Guid Id, string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, decimal TotalAmount,
    int Status, string? Note, Guid? RoomClassId);

/// <summary>Bộ lọc danh sách booking dịch vụ (bám hệ cũ): loại · NCC · trạng thái · khoảng ngày bắt đầu · từ khoá.</summary>
public sealed record ServiceBookingListFilter(
    string? Q = null, ServiceBookingType? Type = null, Guid? ProviderId = null, Guid? OrderId = null,
    int? Status = null, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null);

/// <summary>Thẻ thống kê đầu màn Booking &amp; Dịch vụ: tổng + đếm theo loại + tổng tiền.</summary>
public sealed record ServiceBookingStatsDto(
    int Total, int Hotel, int Flight, int Visa, int Ticket, int Transfer, int Other, decimal TotalAmount);

public sealed record CreateServiceBookingDto(
    string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, int Status, string? Note,
    Guid? RoomClassId = null);

public sealed record UpdateServiceBookingDto(
    string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, int Status, string? Note,
    Guid? RoomClassId = null);
