using System.Text.Json;
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
        Assert.Equal(60, deepseek.TimeoutSeconds);
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
    /// <c>appsettings.json</c> chứa khoá thật nên PHẢI nằm ngoài git. Bỏ dòng ignore đi là lần commit
    /// kế tiếp đẩy khoá lên remote, và không có gì khác trong quy trình bắt được việc đó.
    /// </summary>
    [Fact]
    public void Appsettings_that_phai_duoc_gitignore()
    {
        var ignore = File.ReadAllText(Path.Combine(RepoRoot(), ".gitignore"));

        Assert.Contains("src/TourKit.Api/appsettings.json", ignore, StringComparison.Ordinal);
    }

    /// <summary>File mẫu nằm TRONG git nên tuyệt đối không được có khoá thật.</summary>
    [Fact]
    public void File_mau_khong_duoc_chua_khoa_that()
    {
        var text = File.ReadAllText(ExamplePath());

        Assert.DoesNotContain("\"ApiKey\": \"sk-", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ApiKey\": \"ENC:", text, StringComparison.Ordinal);
        Assert.Contains("\"Secret\": \"\"", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// File mẫu phải có đủ mọi khoá của file thật.
    ///
    /// Đây là cái giá của việc đưa appsettings.json ra khỏi git: thêm một khoá cấu hình mà quên cập
    /// nhật file mẫu thì máy của người khác thiếu khoá đó, và triệu chứng là một tính năng không chạy
    /// chứ không phải một thông báo lỗi. Bài này bắt ngay lúc chạy test.
    /// </summary>
    [Fact]
    public void File_mau_khong_duoc_thieu_khoa_nao_so_voi_file_that()
    {
        var real = LeafKeys(RealPath());
        var example = LeafKeys(ExamplePath());

        var missing = real.Except(example, StringComparer.Ordinal).Order().ToList();

        Assert.True(missing.Count == 0,
            "appsettings.example.json thiếu: " + string.Join(", ", missing));
    }

    private static string RealPath() => Path.Combine(RepoRoot(), "src", "TourKit.Api", "appsettings.json");

    private static string ExamplePath() => Path.Combine(RepoRoot(), "src", "TourKit.Api", "appsettings.example.json");

    /// <summary>Đi ngược lên tới thư mục chứa TourKit.sln — không phụ thuộc chỗ chạy test.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TourKit.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }

    /// <summary>Đường dẫn mọi khoá lá trong file JSON, dạng "Ai:Providers:deepseek:Kind".</summary>
    private static HashSet<string> LeafKeys(string path)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        using var doc = JsonDocument.Parse(
            File.ReadAllText(path),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        Walk(doc.RootElement, "", keys);
        return keys;
    }

    private static void Walk(JsonElement element, string prefix, HashSet<string> keys)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            keys.Add(prefix);
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            // Khoá bắt đầu bằng "//" là ghi chú trong file mẫu, không phải cấu hình.
            if (property.Name.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            Walk(property.Value, prefix.Length == 0 ? property.Name : $"{prefix}:{property.Name}", keys);
        }
    }
}
