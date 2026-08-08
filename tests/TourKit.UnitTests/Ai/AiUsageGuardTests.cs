using Microsoft.Extensions.Options;
using TourKit.Ai.Abstractions;
using TourKit.Api.Ai;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Hạn mức trợ lý. Thứ nó bảo vệ là ngân sách: một vòng lặp gọi công cụ hỏng có thể đốt hết hạn mức
/// tháng trong một buổi.
/// </summary>
public class AiUsageGuardTests
{
    /// <summary>Đồng hồ giả để nhảy phút/ngày mà không phải chờ thật.</summary>
    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (AiUsageGuard Guard, FakeClock Clock) Build(int dailyTokens = 1000, int perMinute = 3)
    {
        var options = new AiOptions();
        options.Limits.DailyTokensPerUser = dailyTokens;
        options.Limits.RequestsPerMinutePerUser = perMinute;

        var clock = new FakeClock(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
        return (new AiUsageGuard(new OptionsWrapper<AiOptions>(options).AsMonitor(), clock), clock);
    }

    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public void Trong_han_muc_thi_cho_qua()
    {
        var (guard, _) = Build();

        Assert.Null(guard.Reject(User));
    }

    [Fact]
    public void Het_token_trong_ngay_thi_chan_va_noi_ro()
    {
        var (guard, _) = Build(dailyTokens: 1000, perMinute: 0);
        guard.Record(User, 1000);

        var refusal = guard.Reject(User);

        Assert.NotNull(refusal);
        Assert.Contains("hết hạn mức", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void Nguoi_khac_khong_bi_anh_huong()
    {
        var (guard, _) = Build(dailyTokens: 1000, perMinute: 0);
        guard.Record(User, 5000);

        Assert.Null(guard.Reject(Guid.NewGuid()));
    }

    [Fact]
    public void Sang_ngay_moi_thi_han_muc_dat_lai()
    {
        var (guard, clock) = Build(dailyTokens: 1000, perMinute: 0);
        guard.Record(User, 5000);
        Assert.NotNull(guard.Reject(User));

        clock.Now = clock.Now.AddDays(1);

        Assert.Null(guard.Reject(User));
        Assert.Equal(0, guard.TokensUsedToday(User));
    }

    [Fact]
    public void Hoi_qua_nhanh_trong_mot_phut_thi_chan()
    {
        var (guard, _) = Build(dailyTokens: 0, perMinute: 3);

        Assert.Null(guard.Reject(User));
        Assert.Null(guard.Reject(User));
        Assert.Null(guard.Reject(User));

        var refusal = guard.Reject(User);

        Assert.NotNull(refusal);
        Assert.Contains("hơi nhanh", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void Sang_phut_moi_thi_dem_lai_tu_dau()
    {
        var (guard, clock) = Build(dailyTokens: 0, perMinute: 1);
        Assert.Null(guard.Reject(User));
        Assert.NotNull(guard.Reject(User));

        clock.Now = clock.Now.AddMinutes(1);

        Assert.Null(guard.Reject(User));
    }

    /// <summary>Đặt 0 = không giới hạn, dùng khi muốn tắt hẳn hạn mức.</summary>
    [Fact]
    public void Han_muc_bang_khong_nghia_la_khong_gioi_han()
    {
        var (guard, _) = Build(dailyTokens: 0, perMinute: 0);
        guard.Record(User, 10_000_000);

        for (var i = 0; i < 50; i++)
        {
            Assert.Null(guard.Reject(User));
        }
    }

    /// <summary>
    /// Nhiều request song song cùng một người: bộ đếm phải cộng đủ. Cộng bằng đọc-rồi-ghi thường sẽ
    /// mất số ở đây.
    /// </summary>
    [Fact]
    public void Ghi_song_song_khong_mat_token()
    {
        var (guard, _) = Build(dailyTokens: 0, perMinute: 0);

        Parallel.For(0, 500, _ => guard.Record(User, 10));

        Assert.Equal(5000, guard.TokensUsedToday(User));
    }
}

/// <summary>Bọc IOptions thành IOptionsMonitor cho test — guard đọc CurrentValue để nhận cấu hình nóng.</summary>
internal static class OptionsMonitorTestExtensions
{
    public static IOptionsMonitor<T> AsMonitor<T>(this IOptions<T> options) where T : class
        => new StaticMonitor<T>(options.Value);

    private sealed class StaticMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
