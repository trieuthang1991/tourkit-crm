using System.Net;
using TourKit.Tests.Support;

namespace TourKit.Tests.Web;

public class RazorPagesSmokeTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public RazorPagesSmokeTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/Ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("pong", html, StringComparison.OrdinalIgnoreCase);
    }
}
