namespace TourKit.Application.Auth;

public enum ExternalAuthStatus
{
    Authenticated,
    NeedsOnboarding,
    Rejected,
}

public sealed record ExternalIdentity(
    string Provider,
    string Subject,
    string Email,
    bool EmailVerified,
    string? DisplayName);

public sealed record ExternalAuthOutcome(
    ExternalAuthStatus Status,
    Guid? UserId = null,
    string? Error = null);
