using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IEnumerable<string> permissions);
    string CreateRefreshToken();
    DateTimeOffset AccessTokenExpiry();
}
