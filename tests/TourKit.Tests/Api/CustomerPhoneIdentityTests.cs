using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Application.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>
/// Khách hàng định danh bằng SỐ ĐIỆN THOẠI.
///
/// Luật này vốn chỉ nằm ở trang Razor (/khach-hang gọi FindByPhoneAsync trước khi lưu), nên
/// POST /api/v1/customers gọi thẳng CustomerService.CreateAsync là đi vòng qua được — API tạo ra
/// đúng thứ giao diện đang chặn. Bài này chốt việc luật đã chuyển xuống tầng dịch vụ.
///
/// Họ tên KHÔNG phải khoá định danh: trùng tên là chuyện bình thường và phải lưu được.
/// </summary>
public class CustomerPhoneIdentityTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CustomerPhoneIdentityTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (_, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static Task<HttpResponseMessage> Tao(HttpClient c, string ten, string? sdt) =>
        c.PostAsJsonAsync("/api/v1/customers", new { FullName = ten, Phone = sdt, CustomerType = 0 });

    [Fact]
    public async Task Qua_API_trung_so_dien_thoai_thi_bi_chan()
    {
        var client = await LoggedInClientAsync("sdt-trung");

        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Nguyễn Văn A", "0912345678")).StatusCode);

        var lan2 = await Tao(client, "Người khác hẳn", "0912345678");
        Assert.Equal(HttpStatusCode.Conflict, lan2.StatusCode);
    }

    /// <summary>+84912… và 0912… là MỘT người — so theo cột đã chuẩn hoá, không so chuỗi thô.</summary>
    [Fact]
    public async Task Cung_so_nhung_khac_dinh_dang_van_tinh_la_trung()
    {
        var client = await LoggedInClientAsync("sdt-chuan-hoa");

        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Khách gốc", "0912345678")).StatusCode);

        var lan2 = await Tao(client, "Cũng người đó", "+84912345678");
        Assert.Equal(HttpStatusCode.Conflict, lan2.StatusCode);
    }

    [Fact]
    public async Task Trung_ho_ten_thi_van_luu_duoc()
    {
        var client = await LoggedInClientAsync("ten-trung");

        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Nguyễn Văn A", "0900000001")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Nguyễn Văn A", "0900000002")).StatusCode);
    }

    /// <summary>Không có số thì không có gì để định danh — nhiều khách để trống SĐT phải lưu được.</summary>
    [Fact]
    public async Task Khong_co_so_dien_thoai_thi_khong_chan()
    {
        var client = await LoggedInClientAsync("sdt-trong");

        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Khách A", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Khách B", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Tao(client, "Khách C", "")).StatusCode);
    }

    /// <summary>Sửa chính mình mà giữ nguyên số thì không được tự coi là trùng với chính nó.</summary>
    [Fact]
    public async Task Sua_chinh_minh_giu_nguyen_so_thi_khong_bi_chan()
    {
        var client = await LoggedInClientAsync("sdt-tu-sua");

        var tao = await Tao(client, "Khách sửa tên", "0987654321");
        Assert.Equal(HttpStatusCode.Created, tao.StatusCode);
        var id = (await tao.Content.ReadFromJsonAsync<Dong>())!.Id;

        var sua = await client.PutAsJsonAsync($"/api/v1/customers/{id}",
            new { FullName = "Khách đã đổi tên", Phone = "0987654321", CustomerType = 0 });
        Assert.Equal(HttpStatusCode.NoContent, sua.StatusCode);
    }

    private sealed record Dong(Guid Id);
}
