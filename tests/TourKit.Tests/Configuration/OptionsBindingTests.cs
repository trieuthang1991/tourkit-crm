using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TourKit.Api.Configuration;
using TourKit.Application.Auth;
using TourKit.Application.Files;
using TourKit.Caching;
using TourKit.Infrastructure.Notifications;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Storage;
using TourKit.Tests.Support;

namespace TourKit.Tests.Configuration;

/// <summary>
/// Mọi section cấu hình phải TIÊM ĐƯỢC từ container thật.
///
/// Quên một dòng đăng ký thì <c>IOptions&lt;T&gt;</c> vẫn giải quyết được — nó trả về đối tượng toàn
/// giá trị mặc định thay vì ném lỗi. Triệu chứng là một tính năng chạy sai âm thầm (gửi mail bằng
/// host rỗng, cache không dùng Redis), nên phải kiểm tra giá trị chứ không chỉ kiểm tra resolve được.
/// </summary>
public class OptionsBindingTests(AuthTestFactory factory) : IClassFixture<AuthTestFactory>
{
    private T Resolve<T>() where T : class
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptions<T>>().Value;
    }

    [Fact]
    public void Moi_section_deu_tiem_duoc()
    {
        Assert.NotNull(Resolve<JwtOptions>());
        Assert.NotNull(Resolve<EmailOptions>());
        Assert.NotNull(Resolve<SmsOptions>());
        Assert.NotNull(Resolve<ZaloOptions>());
        Assert.NotNull(Resolve<DatabaseOptions>());
        Assert.NotNull(Resolve<FileStorageOptions>());
        Assert.NotNull(Resolve<RedisOptions>());
        Assert.NotNull(Resolve<CorsOptions>());
        Assert.NotNull(Resolve<BackgroundJobsOptions>());
    }

    /// <summary>Đọc được GIÁ TRỊ THẬT, không phải mặc định của lớp — đây mới là bằng chứng đã nạp đúng.</summary>
    [Fact]
    public void Doc_dung_gia_tri_trong_appsettings_chu_khong_phai_mac_dinh()
    {
        Assert.Equal("tourkit", Resolve<JwtOptions>().Issuer);
        Assert.False(string.IsNullOrWhiteSpace(Resolve<JwtOptions>().Secret));
        Assert.False(string.IsNullOrWhiteSpace(Resolve<DatabaseOptions>().Provider));
    }

    /// <summary>
    /// Giá trị mặc định nằm TRONG lớp cấu hình, nên nơi thứ hai cần cùng đường dẫn không phải đoán lại.
    /// </summary>
    [Fact]
    public void Duong_dan_luu_tep_luon_co_gia_tri_du_cau_hinh_bo_trong()
    {
        var root = Resolve<FileStorageOptions>().ResolveLocalRoot();

        Assert.False(string.IsNullOrWhiteSpace(root));
        Assert.True(Path.IsPathRooted(root), $"Đường dẫn phải là tuyệt đối, đang là \"{root}\".");
    }

    [Fact]
    public void Danh_sach_origin_luon_co_gia_tri_du_cau_hinh_bo_trong()
    {
        Assert.NotEmpty(Resolve<CorsOptions>().ResolveOrigins());
    }

    /// <summary>
    /// Dây DI của kho tệp: nó được dựng bằng factory đọc <see cref="FileStorageOptions"/>, nên đứt dây
    /// ở đây thì mọi thao tác tải ảnh lên hỏng — mà không bài kiểm thử đơn vị nào thấy, vì chúng dùng
    /// bản giả.
    /// </summary>
    [Fact]
    public void Kho_tep_dung_duoc_tu_container_that()
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IFileStorage>());
    }
}
