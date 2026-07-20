using System.Security.Claims;

namespace TourKit.Api.Auth;

public interface ICookieAuthService
{
    /// <summary>Xác thực và dựng ClaimsPrincipal (scheme cookie) hoặc null nếu sai.</summary>
    Task<ClaimsPrincipal?> AuthenticateAsync(string tenantSlug, string email, string password);
}
