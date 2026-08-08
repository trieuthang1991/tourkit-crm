using System.Security.Claims;
using TourKit.Api.Ai;
using TourKit.Api.Authz;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Hàng rào của các thao tác AI trên bản ghi. Sai ở đây là LỘ DỮ LIỆU: kết quả AI kể lại chính nội
/// dung bản ghi và cả luồng trao đổi nội bộ, thứ thường nhạy cảm hơn bản thân bản ghi.
///
/// Ba tính năng (chấm điểm, tóm tắt, soạn tin) dùng chung đúng hàm này, nên mọi bài dưới đây bảo vệ
/// cả ba cùng lúc.
/// </summary>
public class AiRecordAccessTests
{
    private static readonly Guid Someone = Guid.NewGuid();

    private static ClaimsPrincipal User(params string[] perms)
    {
        var claims = new List<Claim> { new("sub", Someone.ToString()) };
        claims.AddRange(perms.Select(p => new Claim("perm", p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void Du_quyen_thi_cho_qua_va_tra_dung_nguoi_dung()
    {
        var check = AiRecordAccess.Check(User(Permissions.CustomerView), "Customer", Guid.NewGuid().ToString());

        Assert.Null(check.Error);
        Assert.Equal(Someone, check.UserId);
    }

    /// <summary>
    /// Người xem được cơ hội nhưng KHÔNG xem được khách hàng thì không được chấm điểm khách hàng.
    /// Đây là kiểu thủng hay gặp nhất: có quyền với một loại rồi dùng chung endpoint sang loại khác.
    /// </summary>
    [Fact]
    public void Co_quyen_loai_nay_khong_dung_duoc_cho_loai_khac()
    {
        var check = AiRecordAccess.Check(User(Permissions.LeadView), "Customer", Guid.NewGuid().ToString());

        Assert.Equal("Bạn không có quyền xem bản ghi này.", check.Error);
        Assert.Equal(Guid.Empty, check.UserId);
    }

    [Fact]
    public void Khong_co_quyen_nao_thi_tu_choi()
    {
        Assert.NotNull(AiRecordAccess.Check(User(), "Customer", Guid.NewGuid().ToString()).Error);
        Assert.NotNull(AiRecordAccess.Check(User(), "Lead", Guid.NewGuid().ToString()).Error);
        Assert.NotNull(AiRecordAccess.Check(User(), "Order", Guid.NewGuid().ToString()).Error);
    }

    /// <summary>
    /// Loại bản ghi bịa ra phải bị chặn NGAY, trước cả bước kiểm quyền — không có danh sách trắng thì
    /// endpoint thành cửa đọc dữ liệu tuỳ ý.
    /// </summary>
    [Theory]
    [InlineData("Invoice")]
    [InlineData("User")]
    [InlineData("EntityComment")]
    [InlineData("customer")]      // sai hoa thường — danh sách trắng phân biệt hoa thường
    [InlineData("")]
    [InlineData(null)]
    public void Loai_ban_ghi_ngoai_danh_sach_trang_bi_chan(string? entity)
    {
        // Người dùng có TOÀN BỘ quyền vẫn không mở được loại ngoài danh sách.
        var all = User(Permissions.CustomerView, Permissions.LeadView, Permissions.BookingView);

        var check = AiRecordAccess.Check(all, entity, Guid.NewGuid().ToString());

        Assert.Equal("Không xử lý được loại bản ghi này.", check.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Thieu_id_ban_ghi_thi_tu_choi(string? id)
    {
        Assert.NotNull(AiRecordAccess.Check(User(Permissions.CustomerView), "Customer", id).Error);
    }

    /// <summary>
    /// Không đọc được định danh thì TỪ CHỐI, không phải cho qua rồi bỏ đếm hạn mức — một đường vào
    /// không đếm được là một đường vào không giới hạn.
    /// </summary>
    [Fact]
    public void Khong_doc_duoc_dinh_danh_thi_tu_choi_chu_khong_bo_qua_han_muc()
    {
        var noId = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("perm", Permissions.CustomerView)], "test"));

        var check = AiRecordAccess.Check(noId, "Customer", Guid.NewGuid().ToString());

        Assert.Contains("đăng nhập", check.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public void Dinh_danh_khong_phai_guid_thi_tu_choi()
    {
        var bad = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "khong-phai-guid"), new Claim("perm", Permissions.CustomerView)], "test"));

        Assert.NotNull(AiRecordAccess.Check(bad, "Customer", Guid.NewGuid().ToString()).Error);
    }

    /// <summary>Đăng nhập bằng cookie dùng NameIdentifier thay cho "sub" — cả hai phải đọc được.</summary>
    [Fact]
    public void Doc_duoc_dinh_danh_tu_ca_hai_kieu_dang_nhap()
    {
        var cookie = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Someone.ToString()), new Claim("perm", Permissions.CustomerView)],
            "test"));

        var check = AiRecordAccess.Check(cookie, "Customer", Guid.NewGuid().ToString());

        Assert.Null(check.Error);
        Assert.Equal(Someone, check.UserId);
    }

    /// <summary>Ba loại trong danh sách trắng đều dùng đúng mã quyền của màn tương ứng.</summary>
    [Theory]
    [InlineData("Lead", Permissions.LeadView)]
    [InlineData("Customer", Permissions.CustomerView)]
    [InlineData("Order", Permissions.BookingView)]
    public void Moi_loai_doi_dung_ma_quyen_cua_no(string entity, string permission)
    {
        Assert.Null(AiRecordAccess.Check(User(permission), entity, Guid.NewGuid().ToString()).Error);
    }
}
