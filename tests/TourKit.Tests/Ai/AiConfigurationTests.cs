using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TourKit.Ai.Abstractions;
using TourKit.Tests.Support;

namespace TourKit.Tests.Ai;

/// <summary>
/// Soát section <c>Ai</c> THẬT trong appsettings.json, không phải một đối tượng dựng trong test.
///
/// Cần bài này vì <see cref="AiOptions.Validate"/> trả về rỗng cho cả cấu hình đúng lẫn cấu hình
/// không nạp được: nếu binder im lặng trả về bảng rỗng thì mọi bài kiểm tra khác vẫn xanh trong khi
/// hệ thống chạy hoàn toàn không có AI. Ở đây khẳng định có ĐỦ dữ liệu rồi mới khẳng định nó hợp lệ.
/// </summary>
public class AiConfigurationTests(AuthTestFactory factory) : IClassFixture<AuthTestFactory>
{
    private AiOptions Options()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
    }

    [Fact]
    public void Nap_duoc_nha_cung_cap_va_nang_luc_tu_appsettings()
    {
        var options = Options();

        Assert.Contains("deepseek", options.Providers.Keys);
        Assert.Contains("log", options.Providers.Keys);

        var deepseek = options.Providers["deepseek"];
        Assert.Equal("OpenAiCompatible", deepseek.Kind);
        Assert.Equal("https://api.deepseek.com/v1", deepseek.BaseUrl);
        Assert.Contains("Chat", deepseek.Capabilities);   // danh sách chỉ-đọc phải được binder đổ vào
    }

    [Fact]
    public void Moi_tinh_nang_trong_danh_muc_deu_co_mat_trong_appsettings()
    {
        var options = Options();

        foreach (var feature in AiFeatures.All)
        {
            Assert.True(options.Features.ContainsKey(feature),
                $"Thiếu Ai:Features:{feature} trong appsettings.json");
        }
    }

    [Fact]
    public void Tro_ly_dang_tro_vao_DeepSeek()
    {
        var settings = Options().Features[AiFeatures.Assistant];

        Assert.Equal("deepseek", settings.Provider);
        Assert.False(string.IsNullOrWhiteSpace(settings.Model));
    }

    [Fact]
    public void Cau_hinh_that_khong_co_loi_nao()
    {
        Assert.Empty(Options().Validate());
    }

    /// <summary>
    /// Khoá thật không được nằm trong appsettings.json — file đó nằm trong git. Bài này canh đúng
    /// một điều: có ai dán khoá vào file rồi commit hay không.
    /// </summary>
    [Fact]
    public void Khong_co_khoa_that_nao_nam_trong_file_cau_hinh_trong_git()
    {
        var appsettings = Path.Combine(
            Directory.GetCurrentDirectory(), "appsettings.json");
        if (!File.Exists(appsettings))
        {
            return;   // chạy ngoài thư mục nội dung của Api — không có gì để soát
        }

        var text = File.ReadAllText(appsettings);
        var start = text.IndexOf("\"Ai\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "appsettings.json không còn section Ai");

        var aiSection = text[start..];
        Assert.DoesNotContain("\"ApiKey\": \"sk-", aiSection, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ApiKey\": \"ENC:", aiSection, StringComparison.Ordinal);
    }
}
