using TourKit.Shared.Security;

namespace TourKit.Api.Auth;

/// <summary>Đọc userId từ claim "sub" của HttpContext hiện tại (scoped/request).</summary>
public sealed class CurrentUser : ICurrentUser, ICurrentUserContext
{
    public CurrentUser(IHttpContextAccessor accessor)
    {
        var sub = accessor.HttpContext?.User.FindFirst("sub")?.Value
                  ?? accessor.HttpContext?.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        UserId = Guid.TryParse(sub, out var id) ? id : null;
    }

    public Guid? UserId { get; }
}
