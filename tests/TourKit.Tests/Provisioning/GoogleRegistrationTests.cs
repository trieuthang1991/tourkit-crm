using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Application.Auth;
using TourKit.Application.Provisioning;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

namespace TourKit.Tests.Provisioning;

/// <summary>
/// Cấp phát công ty cho người đăng ký bằng Google. Mọi khẳng định dưới đây đọc dữ liệu THẬT trong
/// CSDL sau khi lưu, không đếm số lần gọi hàm.
/// </summary>
public class GoogleRegistrationTests
{
    private static RegisterExternalTenantRequest YeuCau(string slug, string email) =>
        new(CompanyName: $"Công ty {slug}", Slug: slug, AdminEmail: email,
            AdminFullName: "Nguyễn Văn An", Provider: "Google", ProviderSubject: "sub-" + slug);

    [Fact]
    public async Task Tao_du_tenant_admin_quyen_goi_va_lien_ket_Google()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ketQua = await svc.RegisterExternalAsync(YeuCau("cty-gg", "an@cty-gg.vn"));

        Assert.Equal(RegistrationError.None, ketQua.Error);
        Assert.NotNull(ketQua.Response);

        var userId = ketQua.Response!.AdminUserId;
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        Assert.Equal("an@cty-gg.vn", user.Email);

        // Liên kết phải nằm cùng tenant với user, nếu không lần đăng nhập sau sẽ không tìm thấy.
        var lienKet = await db.UserExternalLogins.FirstAsync(l => l.UserId == userId);
        Assert.Equal("Google", lienKet.Provider);
        Assert.Equal("sub-cty-gg", lienKet.ProviderSubject);
        Assert.Equal(ketQua.Response.TenantId, lienKet.TenantId);

        // Người tạo công ty phải là quản trị đủ quyền, nếu không họ đăng nhập xong không làm được gì.
        var soQuyen = await db.UserRoles.Where(ur => ur.UserId == userId)
            .Join(db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp).CountAsync();
        Assert.True(soQuyen > 0, "Tài khoản tạo qua Google không được gán quyền nào.");
    }

    /// <summary>
    /// Cột mật khẩu là bắt buộc, nên tài khoản Google vẫn phải có một chuỗi băm. Điều quan trọng là
    /// chuỗi đó KHÔNG đoán được: nếu dùng một mật khẩu cố định ghi trong mã nguồn thì chỉ cần một
    /// người đọc mã là mọi tài khoản Google trên mọi bản cài đặt đều mở được bằng mật khẩu.
    /// </summary>
    [Fact]
    public async Task Mat_khau_la_chuoi_ngau_nhien_khong_doan_duoc()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var a = await svc.RegisterExternalAsync(YeuCau("cty-a", "a@cty-a.vn"));
        var b = await svc.RegisterExternalAsync(YeuCau("cty-b", "b@cty-b.vn"));

        // IgnoreQueryFilters vì bài này tạo HAI công ty: sau lần thứ hai, ngữ cảnh tenant trỏ vào
        // công ty B nên đọc bình thường sẽ không thấy user của công ty A. Đây là bộ lọc đa đơn vị
        // làm đúng việc của nó, không phải lỗi cần chữa.
        var userA = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == a.Response!.AdminUserId);
        var userB = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == b.Response!.AdminUserId);

        Assert.False(string.IsNullOrWhiteSpace(userA.PasswordHash));

        // Hai tài khoản không được chung một mật khẩu — chung nghĩa là có một giá trị cố định đâu đó.
        Assert.NotEqual(userA.PasswordHash, userB.PasswordHash);

        // Và không khớp với những chuỗi mà người ta hay đặt cho trường hợp này.
        foreach (var doan in new[] { "Google@123", "google", "", "123456789", "P@ssw0rd!" })
        {
            Assert.False(hasher.Verify(userA.PasswordHash, doan),
                $"Mật khẩu tài khoản Google mở được bằng \"{doan}\".");
        }
    }

    [Fact]
    public async Task Trung_email_thi_bao_loi_va_khong_de_lai_cong_ty_dang_do()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await svc.RegisterExternalAsync(YeuCau("cty-mot", "trung@vidu.vn"));
        var lanHai = await svc.RegisterExternalAsync(YeuCau("cty-hai", "trung@vidu.vn"));

        Assert.Equal(RegistrationError.EmailTaken, lanHai.Error);
        Assert.Null(lanHai.Response);

        // Bằng chứng không có rác: công ty thứ hai không được tồn tại.
        Assert.False(await db.Tenants.AnyAsync(t => t.Slug == "cty-hai"));
    }

    [Fact]
    public async Task Trung_ma_doanh_nghiep_thi_bao_loi_rieng()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();

        await svc.RegisterExternalAsync(YeuCau("cty-trung", "mot@vidu.vn"));
        var lanHai = await svc.RegisterExternalAsync(YeuCau("cty-trung", "hai@vidu.vn"));

        Assert.Equal(RegistrationError.SlugTaken, lanHai.Error);
    }

    /// <summary>Thiếu nhà cung cấp/subject thì không có gì để liên kết — từ chối, không tạo công ty.</summary>
    [Fact]
    public async Task Thieu_thong_tin_nha_cung_cap_thi_tu_choi()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ketQua = await svc.RegisterExternalAsync(
            YeuCau("cty-thieu", "x@vidu.vn") with { ProviderSubject = "" });

        Assert.Equal(RegistrationError.Invalid, ketQua.Error);
        Assert.False(await db.Tenants.AnyAsync(t => t.Slug == "cty-thieu"));
    }

    /// <summary>Đăng ký bằng mật khẩu KHÔNG được tạo liên kết nhà cung cấp ngoài nào.</summary>
    [Fact]
    public async Task Dang_ky_thuong_khong_sinh_lien_ket_ngoai()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ketQua = await svc.RegisterAsync(new RegisterTenantRequest(
            "Công ty thường", "cty-thuong", "thuong@vidu.vn", "MatKhau@123", "Trần Thị Bình"));

        Assert.Equal(RegistrationError.None, ketQua.Error);
        Assert.False(await db.UserExternalLogins.AnyAsync(l => l.UserId == ketQua.Response!.AdminUserId));
    }
}
