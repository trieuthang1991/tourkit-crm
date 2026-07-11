using TourKit.Shared.Enums;

namespace TourKit.Application.Booking.Dtos;

public sealed record ServiceBookingDto(
    Guid Id, string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, decimal TotalAmount,
    int Status, string? Note);

public sealed record CreateServiceBookingDto(
    string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, int Status, string? Note);

public sealed record UpdateServiceBookingDto(
    string Code, ServiceBookingType Type, Guid? OrderId, Guid? ProviderId, string Description,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Quantity, decimal UnitPrice, int Status, string? Note);
