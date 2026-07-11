namespace TourKit.Application.Notifications;

public sealed record NotificationDto(
    Guid Id, string Title, string? Message, string? LinkUrl, bool IsRead, DateTimeOffset CreatedAt);
