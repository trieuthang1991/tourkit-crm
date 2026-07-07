namespace TourKit.Api.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct);
}
