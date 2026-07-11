namespace TourKit.Application.Audit.Dtos;

public sealed record ActivityLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityName,
    string EntityId,
    string? Changes,
    DateTimeOffset CreatedAt);
