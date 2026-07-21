using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace TourKit.Caching;

/// <summary>
/// Hiện thực <see cref="ITkCache"/> trên <c>HybridCache</c> — cách làm cache mặc định từ .NET 9.
///
/// Hơn cách tự viết trên IDistributedCache ở ba điểm:
/// • HAI TẦNG: L1 trong bộ nhớ tiến trình + L2 là Redis. Lần đọc lặp lại không phải đi qua mạng.
/// • CHỐNG DẪM CHÂN (stampede): 100 request cùng lúc vào một khoá vừa hết hạn thì chỉ MỘT request
///   chạy truy vấn thật, số còn lại chờ kết quả đó. Tự viết cache-aside sẽ để cả 100 cùng đánh DB.
/// • Tự lo serialize, không phải tay bo JSON.
///
/// BỀN VỚI LỖI HẠ TẦNG: Redis nằm ngoài máy chủ ứng dụng, mạng chập hoặc Redis chết là chuyện bình
/// thường. Cache chỉ để chạy nhanh hơn chứ không phải nguồn dữ liệu, nên lỗi cache bị nuốt (có ghi
/// log cảnh báo) và rơi về lấy dữ liệu thật — KHÔNG để Redis chết kéo sập trang.
/// </summary>
public sealed class HybridTkCache : ITkCache
{
    private readonly HybridCache _cache;
    private readonly ILogger<HybridTkCache> _logger;

    public HybridTkCache(HybridCache cache, ILogger<HybridTkCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            return await _cache.GetOrCreateAsync(
                key,
                factory,
                static async (state, _) => await state().ConfigureAwait(false),
                new HybridCacheEntryOptions
                {
                    Expiration = ttl,
                    // L1 giữ ngắn hơn L2: nhiều tiến trình vẫn hội tụ nhanh khi dữ liệu đổi.
                    LocalCacheExpiration = ttl < TimeSpan.FromSeconds(30) ? ttl : TimeSpan.FromSeconds(30),
                }).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Cache hỏng KHÔNG được làm hỏng request — rơi về lấy dữ liệu thật.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Cache {Key} trục trặc — bỏ qua cache, lấy dữ liệu thật.", key);
            return await factory().ConfigureAwait(false);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Xoá cache hỏng thì mục vẫn tự hết hạn theo TTL.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Xoá cache {Key} thất bại — mục sẽ tự hết hạn theo TTL.", key);
        }
    }
}
