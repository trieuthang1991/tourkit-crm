using TourKit.Shared.Entities;

namespace TourKit.Application.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IEnumerable<string> permissions);
    string CreateRefreshToken();
    DateTimeOffset AccessTokenExpiry();
}
