using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

namespace TourKit.Tests.Web;

/// <summary>
/// Trang đăng ký khi KHÔNG có cookie tạm hợp lệ.
///
/// Đây là đường tấn công thẳng nhất vào tính năng: bỏ qua Google, gọi luôn trang đăng ký và tự khai
/// một email bất kỳ, xem hệ thống có tạo công ty cho mình bằng email của người khác không.
/// </summary>
public class GoogleOnboardingPageTests
{
    private static async Task<Dictionary<string, string>> FormDangKyAsync(HttpClient client, string email)
    {
        var html = await client.GetStringAsync("/dang-ky");
        var m = System.Text.RegularExpressions.Regex.Match(
            html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, "Không tìm thấy token antiforgery trên trang đăng ký.");

        return new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = m.Groups[1].Value,
            ["Input.CompanyName"] = "Công ty thử",
            ["Input.Slug"] = "cty-thu-e2e",
            ["Input.AdminFullName"] = "Kẻ thử",
            ["Input.AdminEmail"] = email,
            ["Input.AcceptTerms"] = "true",
        };
    }

    /// <summary>
    /// Không có cookie tạm thì trang chạy ở chế độ thường: vẫn đòi mật khẩu. Bỏ trống mật khẩu rồi
    /// gửi lên thì không được tạo công ty nào — nếu tạo, nghĩa là chế độ Google bị kích hoạt chỉ bởi
    /// dữ liệu người dùng gửi lên.
    /// </summary>
    [Fact]
    public async Task Khong_co_cookie_tam_thi_khong_tao_duoc_cong_ty_neu_thieu_mat_khau()
    {
        using var factory = new AuthTestFactory { GoogleConfigured = true };
        using var client = factory.CreateClient();

        var form = await FormDangKyAsync(client, "nguoi-khac@vidu.vn");
        var res = await client.PostAsync("/dang-ky", new FormUrlEncodedContent(form));

        // Trang render lại kèm lỗi kiểm tra, không phải chuyển hướng vào hệ thống.
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == "cty-thu-e2e"),
            "Tạo được doanh nghiệp mà không có cookie xác minh lẫn mật khẩu.");
    }

    /// <summary>
    /// Không có cookie tạm thì trang KHÔNG được hiện ở chế độ Google: ô mật khẩu phải còn đó và
    /// tiêu đề vẫn là đăng ký thường.
    /// </summary>
    [Fact]
    public async Task Khong_co_cookie_tam_thi_trang_o_che_do_thuong()
    {
        using var factory = new AuthTestFactory { GoogleConfigured = true };
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/dang-ky");

        Assert.Contains("Input.AdminPassword", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Hoàn tất đăng ký với Google", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Đăng ký thường vẫn phải chạy được sau khi thêm nhánh Google — đây là đường mà đa số người
    /// dùng đi, và nó dễ vỡ nhất khi có người sửa nhánh kia.
    /// </summary>
    [Fact]
    public async Task Dang_ky_bang_mat_khau_van_chay_binh_thuong()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var form = await FormDangKyAsync(client, "chu-moi@vidu.vn");
        form["Input.Slug"] = "cty-mat-khau";
        form["Input.AdminPassword"] = "MatKhau@123";
        var res = await client.PostAsync("/dang-ky", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);   // đã đi theo chuyển hướng về trang đăng nhập

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == "cty-mat-khau"),
            "Đăng ký bằng mật khẩu không tạo được doanh nghiệp.");
    }
}
