using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace TourKit.Caching;

/// <summary>
/// Hiện thực <see cref="ITkCache"/> trên <see cref="IDistributedCache"/> — chạy được cả với Redis
/// lẫn bộ nhớ tiến trình, tuỳ cấu hình lúc đăng ký.
///
/// BỀN VỚI LỖI HẠ TẦNG: Redis nằm ngoài máy chủ ứng dụng, mạng chập hoặc Redis chết là chuyện bình
/// thường. Cache chỉ để chạy nhanh hơn chứ không phải nguồn dữ liệu, nên mọi lỗi đọc/ghi cache đều
/// bị nuốt (có ghi log cảnh báo) và rơi về lấy dữ liệu thật — KHÔNG để Redis chết kéo sập trang.
/// </summary>
public sealed class DistributedTkCache : ITkCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedTkCache> _logger;

    public DistributedTkCache(IDistributedCache cache, ILogger<DistributedTkCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            var cached = await _cache.GetStringAsync(key).ConfigureAwait(false);
            if (cached is not null)
            {
                var value = JsonSerializer.Deserialize<T>(cached, JsonOptions);
                if (value is not null)
                {
                    return value;
                }
            }
        }
#pragma warning disable CA1031 // Cache hỏng KHÔNG được làm hỏng request — rơi về lấy dữ liệu thật.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Đọc cache {Key} thất bại — bỏ qua cache, lấy dữ liệu thật.", key);
        }

        var fresh = await factory().ConfigureAwait(false);

        try
        {
            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(fresh, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Ghi cache hỏng cũng không được làm hỏng request.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Ghi cache {Key} thất bại — bỏ qua.", key);
        }

        return fresh;
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
