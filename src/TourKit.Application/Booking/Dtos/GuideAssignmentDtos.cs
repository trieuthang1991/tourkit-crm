namespace TourKit.Application.Booking.Dtos;

public sealed record GuideAssignmentDto(
    Guid Id,
    Guid TourDepartureId,
    Guid ProviderId,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    DateTimeOffset? TimeReturn,
    string? Note,
    int Status);

public sealed record CreateGuideAssignmentDto(
    Guid TourDepartureId,
    Guid ProviderId,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    DateTimeOffset? TimeReturn,
    string? Note,
    int Status);

public sealed record UpdateGuideAssignmentDto(
    Guid ProviderId,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    DateTimeOffset? TimeReturn,
    string? Note,
    int Status);
