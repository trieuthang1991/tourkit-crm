using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TourKit.Api.Auth;
using TourKit.Infrastructure.Entities;

namespace TourKit.Tests.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService Create()
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "tourkit",
            Audience = "tourkit",
            Secret = "test-secret-key-at-least-32-characters-long!",
            AccessTokenMinutes = 30,
        });
        return new JwtTokenService(opt);
    }

    [Fact]
    public void Access_token_carries_tenant_and_user_claims()
    {
        var svc = Create();
        var user = new User { TenantId = Guid.NewGuid(), Email = "a@b.com", FullName = "A" };

        var token = svc.CreateAccessToken(user, ["tour.create", "customer.view"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.First(c => c.Type == "tenant_id").Value);
        Assert.Equal("a@b.com", jwt.Claims.First(c => c.Type == "email").Value);

        var perms = jwt.Claims.Where(c => c.Type == "perm").Select(c => c.Value).ToList();
        Assert.Contains("tour.create", perms);
        Assert.Contains("customer.view", perms);
    }

    [Fact]
    public void Refresh_token_is_random_and_long()
    {
        var svc = Create();
        var a = svc.CreateRefreshToken();
        var b = svc.CreateRefreshToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 32);
    }
}
