namespace TourKit.Api.Auth;

public sealed record LoginRequest(string TenantSlug, string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
