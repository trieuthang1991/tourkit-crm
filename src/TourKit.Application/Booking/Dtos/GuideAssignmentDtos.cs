namespace TourKit.Application.Booking.Dtos;

public sealed record GuideAssignmentDto(
    Guid Id,
    Guid TourDepartureId,
    Guid ProviderId,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    DateTimeOffset? TimeReturn,
    string? Note,
    int Status,
    string? HandoverContent,
    DateTimeOffset? HandedOverAt);

/// <summary>HDV nộp biên bản bàn giao sau tour.</summary>
public sealed record HandoverDto(string Content);

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
