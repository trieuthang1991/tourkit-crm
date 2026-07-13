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
    DateTimeOffset? HandedOverAt,
    string? ProviderName = null,      // tên HDV (Provider)
    string? DepartureTitle = null,    // tên chuyến
    string? DepartureCode = null);

/// <summary>Bộ lọc lịch điều HDV (bám hệ cũ): HDV · chuyến · trạng thái · khoảng ngày đi.</summary>
public sealed record GuideAssignmentListFilter(
    Guid? ProviderId = null, Guid? DepartureId = null, int? Status = null,
    DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null);

/// <summary>Thẻ thống kê đầu màn Lịch điều HDV: tổng + theo trạng thái + số HDV.</summary>
public sealed record GuideAssignmentStatsDto(int Total, int Created, int Active, int GuideCount);

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
