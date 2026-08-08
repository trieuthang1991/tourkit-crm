using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Options;
using TourKit.Ai.Abstractions;

namespace TourKit.Api.Ai;

/// <summary>
/// Hạn mức dùng trợ lý theo người: token mỗi ngày và số lượt mỗi phút.
///
/// Cái này chặn tình huống một vòng lặp gọi công cụ hỏng đốt hết ngân sách tháng trong một buổi, chứ
/// không phải sổ sách tính tiền. Vì vậy bộ đếm nằm TRONG BỘ NHỚ TIẾN TRÌNH: không thêm bảng, không
/// thêm hạ tầng, và cộng trừ bằng Interlocked nên chính xác khi nhiều request chạy song song.
///
/// Đánh đổi phải biết trước: chạy nhiều tiến trình thì mỗi tiến trình có hạn mức riêng, nên hạn mức
/// thực tế = số tiến trình × cấu hình; khởi động lại thì bộ đếm về 0. Chấp nhận được với thứ nó bảo
/// vệ. Muốn đếm chính xác toàn cụm thì phải có bộ đếm nguyên tử dùng chung (Redis INCR) — lúc đó mới
/// mở rộng <c>ITkCache</c>, đừng làm trước khi thật sự chạy nhiều tiến trình.
/// </summary>
public sealed class AiUsageGuard(IOptionsMonitor<AiOptions> options, TimeProvider clock)
{
    private readonly ConcurrentDictionary<Guid, long> _tokensToday = new();
    private readonly ConcurrentDictionary<Guid, RequestWindow> _requests = new();
    private readonly Lock _rollover = new();

    private DateOnly _day;

    private sealed record RequestWindow(long Minute, int Count);

    /// <summary>
    /// Người này còn được hỏi không. Trả <c>null</c> nếu được; nếu không thì trả câu giải thích tiếng
    /// Việt để hiện thẳng cho người dùng — hết hạn mức phải nói rõ, không được im lặng cắt.
    /// </summary>
    public string? Reject(Guid userId)
    {
        RollOverIfNewDay();
        var limits = options.CurrentValue.Limits;

        if (limits.RequestsPerMinutePerUser > 0)
        {
            var minute = clock.GetUtcNow().ToUnixTimeSeconds() / 60;
            var window = _requests.AddOrUpdate(
                userId,
                _ => new RequestWindow(minute, 1),
                (_, old) => old.Minute == minute ? old with { Count = old.Count + 1 } : new RequestWindow(minute, 1));

            if (window.Count > limits.RequestsPerMinutePerUser)
            {
                return $"Bạn đang hỏi hơi nhanh (quá {limits.RequestsPerMinutePerUser.ToString(CultureInfo.InvariantCulture)} lượt một phút). Chờ một chút rồi hỏi lại nhé.";
            }
        }

        if (limits.DailyTokensPerUser > 0
            && _tokensToday.GetValueOrDefault(userId) >= limits.DailyTokensPerUser)
        {
            return "Bạn đã dùng hết hạn mức trợ lý của hôm nay. Hạn mức đặt lại vào đầu ngày mai, " +
                   "hoặc liên hệ quản trị viên để nâng thêm.";
        }

        return null;
    }

    /// <summary>Ghi nhận token vừa tiêu. Gọi SAU khi có câu trả lời, kể cả khi câu trả lời là lỗi.</summary>
    public void Record(Guid userId, long tokens)
    {
        if (tokens <= 0)
        {
            return;
        }

        RollOverIfNewDay();
        _tokensToday.AddOrUpdate(userId, tokens, (_, old) => old + tokens);
    }

    /// <summary>Số token người này đã dùng hôm nay — để hiện lên màn quản trị sau này.</summary>
    public long TokensUsedToday(Guid userId)
    {
        RollOverIfNewDay();
        return _tokensToday.GetValueOrDefault(userId);
    }

    /// <summary>
    /// Sang ngày mới thì xoá sạch bộ đếm. Đây cũng là chỗ dọn rác: không xoá thì bảng đếm lớn dần theo
    /// số người từng dùng và không bao giờ nhỏ lại.
    /// </summary>
    private void RollOverIfNewDay()
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (_day == today)
        {
            return;
        }

        lock (_rollover)
        {
            if (_day == today)
            {
                return;
            }

            _tokensToday.Clear();
            _requests.Clear();
            _day = today;
        }
    }
}
