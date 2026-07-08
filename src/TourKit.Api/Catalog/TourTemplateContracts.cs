namespace TourKit.Api.Catalog;

public sealed record CreateTourTemplateRequest(
    string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record UpdateTourTemplateRequest(
    string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record TourTemplateResponse(
    Guid Id, string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote, int Status);

public sealed record ItineraryDayRequest(int DayIndex, string Title, string? Detail);
public sealed record ItineraryDayResponse(Guid Id, int DayIndex, string Title, string? Detail);

public sealed record PriceScenarioRequest(int FromQty, int ToQty, decimal UnitPrice);
public sealed record PriceScenarioResponse(Guid Id, int FromQty, int ToQty, decimal UnitPrice);
