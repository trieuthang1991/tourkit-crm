using TourKit.Application.Auth;
using TourKit.Caching;
using TourKit.Infrastructure.Notifications;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Storage;

namespace TourKit.Api.Configuration;

/// <summary>
/// Nơi DUY NHẤT nối các section trong appsettings với lớp cấu hình có kiểu.
///
/// Trước đây phần lớn cấu hình đọc bằng chuỗi ngay tại chỗ dùng (<c>Configuration["FileStorage:LocalRoot"]</c>),
/// nên nơi thứ hai cần cùng giá trị phải chép lại cả tên khoá lẫn giá trị mặc định — và hai bản chép
/// đó lệch nhau lúc nào không ai biết. Gom về đây thì mọi nơi tiêm <c>IOptions&lt;T&gt;</c> là xong,
/// và muốn biết hệ thống có những knob nào thì đọc đúng một file.
/// </summary>
public static class OptionsStartup
{
    /// <summary>Đăng ký toàn bộ section cấu hình để tiêm được ở bất kỳ đâu.</summary>
    public static void AddTourKitOptions(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Bind<JwtOptions>(builder, JwtOptions.SectionName);
        Bind<EmailOptions>(builder, EmailOptions.SectionName);
        Bind<SmsOptions>(builder, SmsOptions.SectionName);
        Bind<ZaloOptions>(builder, ZaloOptions.SectionName);
        Bind<DatabaseOptions>(builder, DatabaseOptions.SectionName);
        Bind<FileStorageOptions>(builder, FileStorageOptions.SectionName);
        Bind<RedisOptions>(builder, RedisOptions.SectionName);
        Bind<CorsOptions>(builder, CorsOptions.SectionName);
        Bind<BackgroundJobsOptions>(builder, BackgroundJobsOptions.SectionName);
        Bind<GoogleAuthOptions>(builder, GoogleAuthOptions.SectionName);

        // Section "Ai" do AiStartup đăng ký, vì nó còn phải soát cấu hình trước khi cho chạy tiếp.
    }

    /// <summary>
    /// Đọc một section NGAY lúc dựng ứng dụng, khi container chưa tồn tại nên chưa tiêm được.
    /// Dùng cho những thứ quyết định chính việc đăng ký dịch vụ: chọn provider CSDL, có bật job nền
    /// hay không, danh sách origin của CORS.
    /// </summary>
    public static T Read<T>(this WebApplicationBuilder builder, string section)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Configuration.GetSection(section).Get<T>() ?? new T();
    }

    private static void Bind<T>(WebApplicationBuilder builder, string section)
        where T : class =>
        builder.Services.Configure<T>(builder.Configuration.GetSection(section));
}
