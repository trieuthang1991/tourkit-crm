namespace TourKit.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest req);
    Task<AuthResponse?> RefreshAsync(string refreshToken);
}
