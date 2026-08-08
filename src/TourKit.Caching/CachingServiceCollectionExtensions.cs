using Microsoft.Extensions.DependencyInjection;
using TourKit.Shared.Security;

namespace TourKit.Caching;

/// <summary>
/// Điểm đăng ký DUY NHẤT của hạ tầng cache. Ứng dụng chỉ gọi <c>AddTourKitCaching(options)</c> và
/// không cần biết gì về Redis — cấu hình quyết định chạy Redis hay bộ nhớ tiến trình.
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Dựng HybridCache (mặc định từ .NET 9): L1 trong bộ nhớ tiến trình, L2 là Redis nếu có cấu hình.
    ///
    /// Có chuỗi kết nối Redis → L2 = Redis, nhiều tiến trình/nhiều máy chủ dùng CHUNG bộ nhớ đệm.
    /// Không có → chỉ còn L1, ứng dụng vẫn chạy bình thường (máy lập trình, môi trường kiểm thử,
    /// hoặc khi cố tình tắt Redis).
    ///
    /// Nhận <see cref="RedisOptions"/> chứ không nhận <c>IConfiguration</c>: thư viện này không cần
    /// biết cấu hình nằm ở section nào hay đọc từ đâu, đó là việc của composition root.
    /// </summary>
    public static IServiceCollection AddTourKitCaching(this IServiceCollection services, RedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Chuỗi kết nối để dạng "ENC:..." trong appsettings, giải mã bằng Crypton như cấu hình SMTP.
        var connection = Crypton.Unwrap(options.ConnectionString);

        if (!string.IsNullOrWhiteSpace(connection))
        {
            // Đăng ký IDistributedCache = Redis TRƯỚC; HybridCache tự nhận nó làm tầng L2.
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = connection;
                o.InstanceName = options.InstanceName;
            });
        }

#pragma warning disable EXTEXP0018 // HybridCache còn gắn nhãn thử nghiệm nhưng đã là cách làm khuyến nghị của .NET 9+.
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        services.AddScoped<ITkCache, HybridTkCache>();
        return services;
    }
}
