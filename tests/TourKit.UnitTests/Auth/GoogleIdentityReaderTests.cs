using System.Security.Claims;
using TourKit.Api.Auth;

namespace TourKit.UnitTests.Auth;

/// <summary>
/// Bộ đọc claim là chốt chặn: mọi thứ lọt qua đây đều được đem đi tra tài khoản và có thể dẫn tới
/// một phiên đăng nhập. Nên phần lớn bài dưới đây kiểm chuyện TỪ CHỐI, không phải chuyện chấp nhận.
/// </summary>
public class GoogleIdentityReaderTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Google"));

    private static ClaimsPrincipal DayDu() => Principal(
        (ClaimTypes.NameIdentifier, "108420"),
        (ClaimTypes.Email, "an@congty.vn"),
        (ClaimTypes.Name, "Nguyễn Văn An"),
        ("email_verified", "true"));

    [Fact]
    public void Principal_day_du_thi_doc_duoc()
    {
        Assert.True(GoogleIdentityReader.TryRead(DayDu(), out var danhTinh));

        Assert.NotNull(danhTinh);
        Assert.Equal("Google", danhTinh!.Provider);
        Assert.Equal("108420", danhTinh.Subject);
        Assert.Equal("an@congty.vn", danhTinh.Email);
        Assert.True(danhTinh.EmailVerified);
        Assert.Equal("Nguyễn Văn An", danhTinh.DisplayName);
    }

    [Fact]
    public void Thieu_subject_thi_tu_choi()
    {
        var p = Principal((ClaimTypes.Email, "an@congty.vn"), ("email_verified", "true"));

        Assert.False(GoogleIdentityReader.TryRead(p, out var danhTinh));
        Assert.Null(danhTinh);
    }

    [Fact]
    public void Thieu_email_thi_tu_choi()
    {
        var p = Principal((ClaimTypes.NameIdentifier, "108420"), ("email_verified", "true"));

        Assert.False(GoogleIdentityReader.TryRead(p, out _));
    }

    /// <summary>
    /// Trường hợp nguy hiểm nhất: Google KHÔNG bảo đảm mọi tài khoản đều có email đã xác minh.
    /// Nhận email chưa xác minh nghĩa là ai khai được email người khác thì chiếm được tài khoản đó.
    /// </summary>
    [Fact]
    public void Email_chua_xac_minh_thi_tu_choi()
    {
        var p = Principal(
            (ClaimTypes.NameIdentifier, "108420"),
            (ClaimTypes.Email, "an@congty.vn"),
            ("email_verified", "false"));

        Assert.False(GoogleIdentityReader.TryRead(p, out _));
    }

    /// <summary>Vắng claim cũng là chưa có bằng chứng xác minh — không phải "mặc định là đã xác minh".</summary>
    [Fact]
    public void Khong_co_claim_email_verified_thi_tu_choi()
    {
        var p = Principal(
            (ClaimTypes.NameIdentifier, "108420"),
            (ClaimTypes.Email, "an@congty.vn"));

        Assert.False(GoogleIdentityReader.TryRead(p, out _));
    }

    /// <summary>
    /// Google trả claim này dưới dạng JSON boolean, ASP.NET ánh xạ ra chuỗi. Hoa/thường tuỳ phiên bản
    /// nên so sánh phải bỏ qua hoa thường — nếu không, một hôm Google đổi "true" thành "True" là
    /// toàn bộ đăng nhập Google im lặng ngừng hoạt động.
    /// </summary>
    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    public void Chu_true_hoa_thuong_deu_chap_nhan(string giaTri)
    {
        var p = Principal(
            (ClaimTypes.NameIdentifier, "108420"),
            (ClaimTypes.Email, "an@congty.vn"),
            ("email_verified", giaTri));

        Assert.True(GoogleIdentityReader.TryRead(p, out _));
    }

    [Fact]
    public void Principal_rong_hoac_null_thi_tu_choi()
    {
        Assert.False(GoogleIdentityReader.TryRead(null, out _));
        Assert.False(GoogleIdentityReader.TryRead(new ClaimsPrincipal(), out _));
    }

    /// <summary>Tên hiển thị là tuỳ chọn — thiếu nó không được làm hỏng cả lần đăng nhập.</summary>
    [Fact]
    public void Thieu_ten_hien_thi_van_doc_duoc()
    {
        var p = Principal(
            (ClaimTypes.NameIdentifier, "108420"),
            (ClaimTypes.Email, "an@congty.vn"),
            ("email_verified", "true"));

        Assert.True(GoogleIdentityReader.TryRead(p, out var danhTinh));
        Assert.Null(danhTinh!.DisplayName);
    }
}
