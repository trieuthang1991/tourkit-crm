using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    DateTimeOffset AccessTokenExpiry();
}
