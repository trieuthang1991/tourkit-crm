namespace TourKit.Application.Crm.Dtos;

public sealed record TourRatingDto(
    Guid Id, Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone,
    int Stars, string? Comment, int Status);

public sealed record CreateTourRatingDto(
    Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);

public sealed record UpdateTourRatingDto(
    string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);
