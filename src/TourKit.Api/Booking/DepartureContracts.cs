namespace TourKit.Api.Booking;

public sealed record CreateDepartureRequest(
    Guid? TemplateId, string Code, string Title,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots);

public sealed record DepartureResponse(
    Guid Id, string Code, string Title, Guid? TemplateId,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots, int Status);
