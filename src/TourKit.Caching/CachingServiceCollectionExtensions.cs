using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Shared.Security;

namespace TourKit.Caching;

/// <summary>
/// Điểm đăng ký DUY NHẤT của hạ tầng cache. Ứng dụng chỉ gọi <c>AddTourKitCaching(configuration)</c>
/// và không cần biết gì về Redis — cấu hình quyết định chạy Redis hay bộ nhớ tiến trình.
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>Khoá cấu hình chứa chuỗi kết nối Redis (chấp nhận cả dạng ENC: mã hoá).</summary>
    public const string ConnectionStringKey = "Redis:ConnectionString";

    /// <summary>Tiền tố khoá trên Redis, để nhiều ứng dụng dùng chung một máy chủ Redis không đụng nhau.</summary>
    public const string InstanceName = "tourkit:";

    /// <summary>
    /// Có chuỗi kết nối Redis → dùng Redis (nhiều tiến trình/nhiều máy chủ dùng CHUNG bộ nhớ đệm).
    /// Không có → rơi về cache trong bộ nhớ tiến trình, ứng dụng vẫn chạy bình thường (máy lập trình,
    /// môi trường kiểm thử, hoặc khi cố tình tắt Redis).
    /// </summary>
    public static IServiceCollection AddTourKitCaching(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Chuỗi kết nối để dạng "ENC:..." trong appsettings, giải mã bằng Crypton như cấu hình SMTP.
        var connection = Crypton.Unwrap(configuration[ConnectionStringKey]);

        if (string.IsNullOrWhiteSpace(connection))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = connection;
                o.InstanceName = InstanceName;
            });
        }

        services.AddScoped<ITkCache, DistributedTkCache>();
        return services;
    }
}
