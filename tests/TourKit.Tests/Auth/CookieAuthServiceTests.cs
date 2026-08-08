using Microsoft.Extensions.DependencyInjection;
using TourKit.Application.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Auth;

public class CookieAuthServiceTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public CookieAuthServiceTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Valid_credentials_return_principal_with_claims()
    {
        var (_, email, password) = await _factory.SeedTenantUserAsync("cookie-ok");
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICookieAuthService>();

        var principal = await svc.AuthenticateAsync($"  {email.ToUpperInvariant()}  ", password);

        Assert.NotNull(principal);
        Assert.False(string.IsNullOrEmpty(principal!.FindFirst("sub")?.Value));
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst("tenant_id")?.Value));
        Assert.Equal(email, principal.FindFirst("email")?.Value);
    }

    [Fact]
    public async Task Wrong_password_returns_null()
    {
        var (_, email, _) = await _factory.SeedTenantUserAsync("cookie-bad");
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICookieAuthService>();

        Assert.Null(await svc.AuthenticateAsync(email, "wrong-password"));
    }
}
