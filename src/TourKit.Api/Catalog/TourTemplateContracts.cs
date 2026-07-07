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
