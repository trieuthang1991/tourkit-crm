using System.Text.RegularExpressions;

namespace TourKit.Tests.Web;

/// <summary>
/// Mọi <c>Url.Page("...")</c> phải trỏ tới một trang Razor CÓ THẬT.
///
/// Bài này sinh ra từ một lỗi thật trên màn chi tiết khách hàng: <c>Url.Page("/khach-hang/Index")</c>
/// — truyền route tiếng Việt vào chỗ cần ĐƯỜNG DẪN TRANG (<c>/CustomersTabulator/Index</c>).
///
/// Cách nó hỏng mới là vấn đề: <c>Url.Page</c> trả về <c>null</c>, không ném lỗi. Chỗ gọi có
/// <c>?? "?handler=Save"</c> nên form âm thầm gửi vào chính trang đang mở, trang đó không có handler
/// Save, Razor trả HTML kèm mã 200, JavaScript đọc JSON thất bại và người dùng thấy "Lỗi kết nối,
/// thử lại". Điền đủ mọi trường bắt buộc vẫn không lưu được, mà thông báo thì dẫn đi sai hướng
/// hoàn toàn.
///
/// Không lớp kiểm thử nào khác bắt được: đường dẫn là chuỗi, chỉ được giải lúc dựng trang.
/// </summary>
public class UrlPageTargetTests
{
    [Fact]
    public void Moi_Url_Page_deu_tro_toi_trang_co_that()
    {
        var pagesDir = Path.Combine(RepoRoot(), "src", "TourKit.Api", "Pages");
        var files = Directory.EnumerateFiles(pagesDir, "*.cs*", SearchOption.AllDirectories);

        var sai = new List<string>();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(
                text, @"Url\.Page\(\s*""(/[^""]+)""", RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                var target = m.Groups[1].Value;
                var cshtml = Path.Combine(pagesDir, target.TrimStart('/').Replace('/', Path.DirectorySeparatorChar) + ".cshtml");

                if (!File.Exists(cshtml))
                {
                    sai.Add($"{Path.GetFileName(file)} → Url.Page(\"{target}\") nhưng không có {target}.cshtml");
                }
            }
        }

        Assert.True(sai.Count == 0,
            "Url.Page trỏ tới trang không tồn tại (sẽ trả null, không ném lỗi):" + Environment.NewLine +
            string.Join(Environment.NewLine, sai));
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
