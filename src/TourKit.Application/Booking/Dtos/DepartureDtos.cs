namespace TourKit.Application.Booking.Dtos;

public sealed record DepartureDto(
    Guid Id, string Code, string Title, Guid? TemplateId,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots, int Status);

public sealed record CreateDepartureDto(
    Guid? TemplateId, string Code, string Title,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots);
