using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TourKit.Ai.Abstractions;

namespace TourKit.Api.Ai;

/// <summary>
/// Composition root của phần AI: đọc cấu hình, chọn adapter theo <c>Kind</c>, dựng client cho từng
/// tính năng. Đây là NƠI DUY NHẤT trong hệ thống biết tính năng nào chạy bằng hãng nào.
///
/// Client được giữ lại và dùng lại: mỗi client ôm một <c>HttpClient</c>, dựng mới theo từng lượt hỏi
/// sẽ làm cạn cổng mạng đúng kiểu <c>new HttpClient()</c> trong vòng lặp.
/// </summary>
public sealed class AiChatClientFactory(
    IEnumerable<IChatClientProvider> providers,
    IOptionsMonitor<AiOptions> options,
    ILogger<AiChatClientFactory> logger)
{
    private readonly ConcurrentDictionary<string, IChatClient> _clients = new(StringComparer.Ordinal);

    private readonly Dictionary<string, IChatClientProvider> _adapters =
        providers.ToDictionary(p => p.Kind, StringComparer.OrdinalIgnoreCase);

    /// <summary>Cấu hình AI đang có hiệu lực.</summary>
    public AiOptions Current => options.CurrentValue;

    /// <summary>
    /// Client + thông số cho một tính năng, hoặc <c>null</c> khi tính năng đang tắt. Trả <c>null</c>
    /// thay vì ném để nơi gọi rẽ sang đường lui — AI hỏng thì màn hình vẫn phải chạy bình thường.
    /// </summary>
    public (IChatClient Client, AiFeatureResolution Config)? For(string feature)
    {
        var resolved = Current.Resolve(feature);
        if (resolved is null)
        {
            return null;
        }

        if (!_adapters.TryGetValue(resolved.Provider.Kind, out var adapter))
        {
            // Cấu hình đã qua bước soát lúc khởi động nên Kind là hợp lệ; tới đây nghĩa là project
            // adapter tương ứng chưa được tham chiếu vào Api.
            logger.LogError(
                "Chưa có adapter nào phục vụ Kind \"{Kind}\" (tính năng {Feature}). Thêm project adapter và đăng ký IChatClientProvider.",
                resolved.Provider.Kind, feature);
            return null;
        }

        // Khoá nằm trong khoá tra: đổi khoá hay đổi địa chỉ lúc chạy sẽ tự sinh client mới, không dùng lại cái cũ.
        var key = string.Create(CultureInfo.InvariantCulture,
            $"{resolved.ProviderName}|{resolved.Model}|{resolved.Provider.BaseUrl}|{resolved.Provider.ApiKey.GetHashCode(StringComparison.Ordinal)}|{resolved.TimeoutSeconds}");

        var client = _clients.GetOrAdd(key, _ => adapter.Create(resolved.Provider, resolved.Model));
        return (client, resolved);
    }
}
