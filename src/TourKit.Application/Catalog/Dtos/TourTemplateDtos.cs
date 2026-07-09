namespace TourKit.Application.Catalog.Dtos;

public sealed record TourTemplateDto(
    Guid Id, string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote, int Status);

public sealed record CreateTourTemplateDto(
    string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record UpdateTourTemplateDto(
    string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record ItineraryDayDto(Guid Id, int DayIndex, string Title, string? Detail);

public sealed record PriceScenarioDto(Guid Id, int FromQty, int ToQty, decimal UnitPrice);
