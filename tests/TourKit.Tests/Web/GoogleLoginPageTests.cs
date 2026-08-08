using System.Net;
using TourKit.Tests.Support;

namespace TourKit.Tests.Web;

/// <summary>
/// Trang đăng nhập ở hai chế độ: chưa cấu hình Google và đã cấu hình.
///
/// Bộ bài này bắt được đúng loại lỗi vừa xảy ra khi viết tính năng: <c>LoginModel</c> nhận thêm một
/// phụ thuộc mà quên đăng ký DI. Biên dịch vẫn sạch, mọi bài test nghiệp vụ vẫn xanh, chỉ có trang
/// đăng nhập trả 500 — nghĩa là không ai vào được hệ thống. Chỉ một bài render thật mới thấy.
/// </summary>
public class GoogleLoginPageTests
{
    /// <summary>
    /// Lấy token antiforgery đúng cách người dùng thật lấy: mở trang rồi đọc ô ẩn trong form. Gửi
    /// POST trần sẽ bị chặn ở 400 trước khi chạm tới handler, và bài test sẽ "đỏ" vì lý do chẳng
    /// liên quan gì tới thứ nó định kiểm.
    /// </summary>
    private static async Task<FormUrlEncodedContent> FormCoTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/dang-nhap");
        var m = System.Text.RegularExpressions.Regex.Match(
            html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, "Không tìm thấy token antiforgery trên trang đăng nhập.");

        return new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = m.Groups[1].Value });
    }

    [Fact]
    public async Task Chua_cau_hinh_Google_thi_trang_van_len_va_khong_co_nut()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/dang-nhap");
        var html = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.DoesNotContain("Tiếp tục với Google", html, StringComparison.Ordinal);
        // Đăng nhập bằng mật khẩu KHÔNG được phụ thuộc vào việc có Google hay không.
        Assert.Contains("Input.Password", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cau_hinh_du_khoa_thi_hien_nut_Google()
    {
        using var factory = new AuthTestFactory { GoogleConfigured = true };
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/dang-nhap");
        var html = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Tiếp tục với Google", html, StringComparison.Ordinal);
        Assert.Contains("handler=Google", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Chưa bật Google thì đường dẫn coi như không tồn tại. Nếu nó trả 500 hoặc một trang lỗi có
    /// nội dung, người ngoài dò được hệ thống có sẵn tính năng gì mà chưa bật.
    /// </summary>
    [Fact]
    public async Task Chua_bat_Google_thi_bam_vao_tra_404()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var res = await client.PostAsync("/dang-nhap?handler=Google", await FormCoTokenAsync(client));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// Đã cấu hình thì bấm nút phải ĐẨY SANG Google, không phải render lại trang. Bài này cũng chứng
    /// minh scheme "Google" thật sự được đăng ký — thiếu nó thì Challenge ném ra lỗi 500.
    /// </summary>
    [Fact]
    public async Task Cau_hinh_du_khoa_thi_bam_nut_chuyen_sang_Google()
    {
        using var factory = new AuthTestFactory { GoogleConfigured = true };
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,   // giữ lại phản hồi 302 để đọc đích đến
            });

        var res = await client.PostAsync("/dang-nhap?handler=Google", await FormCoTokenAsync(client));

        Assert.True(res.StatusCode == HttpStatusCode.Redirect,
            $"Mong 302 sang Google, nhận {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        var dich = res.Headers.Location?.ToString() ?? "";
        Assert.Contains("accounts.google.com", dich, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Không có cookie tạm mà gọi thẳng đường quay về thì phải bị đẩy về trang đăng nhập, tuyệt đối
    /// không được tạo phiên. Đây là đường mà kẻ tấn công sẽ thử đầu tiên: bỏ qua Google, gọi luôn
    /// callback xem hệ thống có nhẹ dạ ký cookie cho không.
    /// </summary>
    [Fact]
    public async Task Goi_thang_duong_quay_ve_khong_co_cookie_thi_khong_tao_phien()
    {
        using var factory = new AuthTestFactory { GoogleConfigured = true };
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        var res = await client.GetAsync("/dang-nhap?handler=GoogleCallback");

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("dang-nhap", res.Headers.Location?.ToString() ?? "", StringComparison.Ordinal);

        // Bằng chứng thật: không có cookie đăng nhập chính nào được phát ra.
        var cookies = res.Headers.TryGetValues("Set-Cookie", out var v) ? string.Join(" ", v) : "";
        Assert.DoesNotContain("tourkit_auth=", cookies, StringComparison.Ordinal);
    }
}
