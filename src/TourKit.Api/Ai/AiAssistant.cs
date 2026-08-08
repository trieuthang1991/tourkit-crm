using System.Security.Claims;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.Api.Ai;

/// <summary>
/// Cửa vào của trợ lý cho tầng giao diện. Gói ba việc mà mọi nơi gọi đều phải làm giống nhau: kiểm tra
/// tính năng có bật không, lấy bộ quyền của người đang hỏi từ claim, và dựng vòng lặp chat đúng thông
/// số cấu hình.
/// </summary>
public sealed class AiAssistant(
    AiChatClientFactory factory,
    AiToolRegistry registry,
    ILoggerFactory loggers)
{
    /// <summary>Tính năng trợ lý có đang bật và có adapter phục vụ được không.</summary>
    public bool IsAvailable => factory.For(AiFeatures.Assistant) is not null;

    /// <summary>
    /// Hỏi trợ lý. Trả <c>null</c> khi tính năng đang tắt — nơi gọi hiển thị thông báo thay vì lỗi.
    ///
    /// Bộ quyền lấy từ claim <c>perm</c> đã ký lúc đăng nhập, KHÔNG tra lại bảng: đó cũng chính là bộ
    /// quyền mà mọi màn hình khác đang dùng, nên trợ lý không thể rộng hơn màn hình.
    /// </summary>
    public async Task<AiAnswer?> AskAsync(string question, ClaimsPrincipal user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var resolved = factory.For(AiFeatures.Assistant);
        if (resolved is null)
        {
            return null;
        }

        var (client, config) = resolved.Value;
        var perms = user.FindAll("perm").Select(c => c.Value).ToHashSet(StringComparer.Ordinal);

        var settings = new AiChatSettings(
            config.Settings.Model,
            config.Settings.MaxOutputTokens,
            config.Settings.MaxToolRounds,
            config.Settings.Temperature);

        var service = new AiChatService(client, registry, settings, loggers.CreateLogger<AiChatService>());
        return await service.AskAsync(question, perms, ct).ConfigureAwait(false);
    }
}
