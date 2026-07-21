using System.Security.Claims;

namespace TourKit.Application.Auth;

public interface ICookieAuthService
{
    /// <summary>Xác thực và dựng ClaimsPrincipal (scheme cookie) hoặc null nếu sai.</summary>
    Task<ClaimsPrincipal?> AuthenticateAsync(string tenantSlug, string email, string password);
}
