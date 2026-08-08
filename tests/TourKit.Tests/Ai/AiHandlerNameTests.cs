using System.Reflection;
using System.Text.RegularExpressions;
using TourKit.Api.Pages.Ai;

namespace TourKit.Tests.Ai;

/// <summary>
/// Tên nút trong JavaScript phải khớp tên handler trong C#.
///
/// Bài này sinh ra từ một lỗi thật: đổi khoá nút từ <c>Run</c> sang <c>Review</c> ở JS mà quên đổi
/// <c>OnPostRunAsync</c> ở C#. Razor Pages KHÔNG báo lỗi khi handler không tồn tại — nó trả về HTML
/// của trang kèm mã 200. JavaScript gọi <c>r.json()</c> trên HTML đó, thất bại, rồi hiện "Lỗi kết
/// nối, thử lại" — một thông báo dẫn người ta đi tìm sai hướng hoàn toàn.
///
/// Không bài kiểm thử đơn vị nào bắt được: hai bên chỉ gặp nhau lúc chạy, qua một chuỗi.
/// </summary>
public class AiHandlerNameTests
{
    [Fact]
    public void Moi_nut_trong_javascript_deu_co_handler_tuong_ung()
    {
        var js = File.ReadAllText(Path.Combine(RepoRoot(), "src", "TourKit.Api", "wwwroot", "js", "tk-ai.js"));

        var keys = Regex.Matches(js, @"data-act=""(\w+)""", RegexOptions.None, TimeSpan.FromSeconds(2))
            .Select(m => m.Groups[1].Value)
            .Concat(Regex.Matches(js, @"key:\s*'(\w+)'", RegexOptions.None, TimeSpan.FromSeconds(2))
                .Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(keys);

        var handlers = typeof(ReviewModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = keys.Where(k => !handlers.Contains($"OnPost{k}Async") && !handlers.Contains($"OnPost{k}"))
            .ToList();

        Assert.True(missing.Count == 0,
            "tk-ai.js gọi handler không tồn tại trong ReviewModel: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Chiều ngược lại: handler viết ra mà JS không gọi thì đó là mã chết — hoặc tệ hơn, một endpoint
    /// còn sống mà không ai nhớ là nó tồn tại.
    /// </summary>
    [Fact]
    public void Khong_co_handler_nao_bi_bo_quen()
    {
        var js = File.ReadAllText(Path.Combine(RepoRoot(), "src", "TourKit.Api", "wwwroot", "js", "tk-ai.js"));

        var handlers = typeof(ReviewModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.StartsWith("OnPost", StringComparison.Ordinal))
            .Select(m => m.Name["OnPost".Length..].Replace("Async", "", StringComparison.Ordinal))
            .ToList();

        var unused = handlers.Where(h => !js.Contains($"'{h}'", StringComparison.Ordinal)
                                      && !js.Contains($"\"{h}\"", StringComparison.Ordinal))
            .ToList();

        Assert.True(unused.Count == 0,
            "ReviewModel có handler không nơi nào gọi: " + string.Join(", ", unused));
    }

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
}
