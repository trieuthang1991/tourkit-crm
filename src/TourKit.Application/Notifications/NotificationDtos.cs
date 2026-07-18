namespace TourKit.Application.Notifications;

public sealed record NotificationDto(
    Guid Id, string Title, string? Message, string? LinkUrl, string Type, bool IsRead, DateTimeOffset CreatedAt);
