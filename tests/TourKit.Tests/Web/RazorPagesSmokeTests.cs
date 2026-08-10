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

    [Fact]
    public async Task Landing_page_renders_export_design_anonymously()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("M-Travel", html, StringComparison.Ordinal);
        Assert.Contains("Cả công ty lữ hành trong một hệ thống.", html, StringComparison.Ordinal);
        Assert.Contains("M-Travel AI", html, StringComparison.Ordinal);
        Assert.Contains("role-tabs", html, StringComparison.Ordinal);
        Assert.Contains("plans", html, StringComparison.Ordinal);
        Assert.Contains("Miễn phí đến 20 nhân viên", html, StringComparison.Ordinal);
        Assert.Contains("60.000đ", html, StringComparison.Ordinal);
        Assert.Contains("chỉ tính từ nhân viên thứ 21", html, StringComparison.Ordinal);
        Assert.Contains("30 nhân viên", html, StringComparison.Ordinal);
        Assert.Contains("600.000đ/tháng", html, StringComparison.Ordinal);
        Assert.Contains("Theo yêu cầu", html, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-annual", html, StringComparison.Ordinal);
        Assert.DoesNotContain("tiết kiệm 2 tháng", html, StringComparison.Ordinal);
        Assert.Contains("faqs", html, StringComparison.Ordinal);
        Assert.Contains("/css/landing.css", html, StringComparison.Ordinal);
        Assert.Contains("/js/landing.js", html, StringComparison.Ordinal);
        Assert.Contains("/img/landing/m-travel-product-walkthrough.webp", html, StringComparison.Ordinal);
        Assert.Contains("/img/landing/m-travel-customer-avatar.webp", html, StringComparison.Ordinal);
        Assert.Contains("/img/landing/m-travel-customer-story.webp", html, StringComparison.Ordinal);
        Assert.Contains("/dang-ky", html, StringComparison.Ordinal);
        Assert.Contains("/dang-nhap", html, StringComparison.Ordinal);
        Assert.DoesNotContain("img-slot", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cdn.tailwindcss.com", html, StringComparison.Ordinal);
        Assert.DoesNotContain("tailwind.config.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("three.module", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_url_301_redirects_to_vietnamese_route()
    {
        // URL cũ PascalCase (/Customers) phải 301 sang route tiếng Việt (/khach-hang) —
        // LegacyRouteRedirectMiddleware. Giữ bookmark/link cũ không chết.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/Customers");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode); // 301
        Assert.Contains("/khach-hang", res.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Protected_page_redirects_to_login_when_anonymous()
    {
        // Trang cần đăng nhập, truy cập ẩn danh → 302 sang trang đăng nhập tiếng Việt.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/khach-hang");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode); // 302 → /dang-nhap
        Assert.Contains("/dang-nhap", res.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/dang-nhap");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("Đăng nhập", html, StringComparison.Ordinal);
        Assert.Contains("M-Travel", html, StringComparison.Ordinal);
        Assert.Contains("/img/illustrations/auth-login.webp", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Input.TenantSlug", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Mã doanh nghiệp", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_page_uses_its_own_auth_visual()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/dang-ky");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("M-Travel", html, StringComparison.Ordinal);
        Assert.Contains("/img/illustrations/auth-register.webp", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Forgot_password_page_asks_for_email_only()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/quen-mat-khau");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("Input.Email", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Input.TenantSlug", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Mã doanh nghiệp", html, StringComparison.OrdinalIgnoreCase);
    }
}
