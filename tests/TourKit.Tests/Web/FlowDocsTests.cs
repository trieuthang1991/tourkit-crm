using TourKit.Api.Pages.Shared;

namespace TourKit.Tests.Web;

/// <summary>
/// Soát LIÊN KẾT giữa sổ đăng ký <see cref="FlowDocs"/> và các file markdown thật trong
/// <c>docs/business/</c>.
///
/// Vì sao cần: nút "?" trên 14 trang dựng đường dẫn từ <c>Doc</c> + <c>Anchor</c>. Đổi tên một mục
/// trong markdown mà quên sửa sổ thì không có gì báo — trang vẫn chạy, nút vẫn hiện, chỉ là bấm vào
/// thì GitHub mở file rồi đứng yên ở đầu trang vì không tìm thấy neo. Hỏng im lặng, và người phát
/// hiện sẽ là người dùng chứ không phải chúng ta.
///
/// Bài này CỐ Ý không kiểm nội dung văn xuôi: văn bản còn sửa nhiều, ràng vào test thì mỗi lần sửa
/// câu chữ lại đỏ một cách vô ích.
/// </summary>
public class FlowDocsTests
{
    [Fact]
    public void Moi_muc_deu_tro_toi_file_markdown_co_that()
    {
        var thieu = FlowDocs.All
            .Select(d => d.Doc)
            .Distinct(StringComparer.Ordinal)
            .Where(doc => !File.Exists(DuongDan(doc)))
            .ToList();

        Assert.True(thieu.Count == 0,
            "Sổ đăng ký trỏ tới file không tồn tại trong docs/business/:" + Environment.NewLine +
            string.Join(Environment.NewLine, thieu.Select(d => $"- {d}.md")));
    }

    [Fact]
    public void Moi_neo_deu_co_mat_trong_file_markdown()
    {
        var noiDung = FlowDocs.All
            .Select(d => d.Doc)
            .Distinct(StringComparer.Ordinal)
            .Where(doc => File.Exists(DuongDan(doc)))
            .ToDictionary(doc => doc, doc => File.ReadAllText(DuongDan(doc)), StringComparer.Ordinal);

        var sai = FlowDocs.All
            .Where(d => !noiDung.TryGetValue(d.Doc, out var text)
                        || !text.Contains($"<a id=\"{d.Anchor}\"></a>", StringComparison.Ordinal))
            .Select(d => $"- {d.Key}: chờ <a id=\"{d.Anchor}\"></a> trong {d.Doc}.md")
            .ToList();

        Assert.True(sai.Count == 0,
            "Neo khai trong sổ đăng ký không có trong markdown (link sẽ mở file nhưng không nhảy tới mục):"
            + Environment.NewLine + string.Join(Environment.NewLine, sai));
    }

    [Fact]
    public void Khoa_khong_duoc_trung_nhau()
    {
        var trung = FlowDocs.All
            .GroupBy(d => d.Key, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(trung.Count == 0, "Khoá trùng trong sổ đăng ký: " + string.Join(", ", trung));
    }

    [Fact]
    public void Moi_muc_deu_co_gioi_thieu_de_hien_tooltip()
    {
        var rong = FlowDocs.All
            .Where(d => string.IsNullOrWhiteSpace(d.Summary) || string.IsNullOrWhiteSpace(d.Title))
            .Select(d => d.Key)
            .ToList();

        Assert.True(rong.Count == 0, "Mục thiếu Title/Summary nên popover sẽ trống: " + string.Join(", ", rong));
    }

    private static string DuongDan(string doc) =>
        Path.Combine(RepoRoot(), "docs", "business", doc + ".md");

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
