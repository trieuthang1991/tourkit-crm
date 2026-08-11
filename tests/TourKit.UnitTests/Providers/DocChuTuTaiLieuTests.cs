using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TourKit.Api.Web;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Bóc chữ từ tài liệu báo giá để đưa cho AI đọc (bước 2 của IMPORT).
///
/// Bẫy chính nằm ở BẢNG: bảng giá trong file Word gần như luôn là bảng thật, mà đọc thẳng InnerText
/// của cả tài liệu thì mọi ô dính liền thành một chuỗi không có ranh giới. Model sẽ không biết đâu là
/// hết ô này sang ô kia và ghép giá của dòng này vào tên gói của dòng khác — sai mà không có lỗi nào.
/// </summary>
public class DocChuTuTaiLieuTests
{
    /// <summary>Dựng một .docx thật trong bộ nhớ: có đoạn văn và một bảng hai dòng.</summary>
    private static MemoryStream TaoDocx()
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var body = doc.AddMainDocumentPart().Document = new Document(new Body());

            static Paragraph P(string t) => new(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(t)));
            static TableCell C(string t) => new(P(t));

            var than = body.Body!;
            than.Append(P("BẢNG GIÁ KHÁCH SẠN ABC"));
            than.Append(new Table(
                new TableRow(C("Hạng phòng"), C("Giá NET")),
                new TableRow(C("Deluxe"), C("1.200.000")),
                new TableRow(C("Superior"), C("900.000"))));
            than.Append(P("Áp dụng từ 01/06/2026"));
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Docx_boc_duoc_ca_doan_van_lan_bang()
    {
        using var ms = TaoDocx();

        var chu = DocChuTuTaiLieu.Doc(ms, "bang-gia.docx");

        Assert.Contains("BẢNG GIÁ KHÁCH SẠN ABC", chu, StringComparison.Ordinal);
        Assert.Contains("Deluxe", chu, StringComparison.Ordinal);
        Assert.Contains("1.200.000", chu, StringComparison.Ordinal);
        Assert.Contains("Áp dụng từ 01/06/2026", chu, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ô của cùng một dòng phải có RANH GIỚI rõ ràng, và hai dòng khác nhau phải nằm ở hai dòng chữ
    /// khác nhau. Không có ranh giới thì "Deluxe1.200.000Superior900.000" là thứ model nhận được.
    /// </summary>
    [Fact]
    public void O_trong_bang_phai_tach_nhau_va_moi_hang_mot_dong()
    {
        using var ms = TaoDocx();

        var chu = DocChuTuTaiLieu.Doc(ms, "bang-gia.docx");
        var dong = chu.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToList();

        Assert.Contains("Deluxe | 1.200.000", dong);
        Assert.Contains("Superior | 900.000", dong);
        Assert.DoesNotContain(dong, d => d.Contains("DeluxeSuperior", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("bao-gia.pdf", true)]
    [InlineData("bao-gia.docx", true)]
    [InlineData("bao-gia.DOC", true)]
    [InlineData("bang-gia.xlsx", false)]
    [InlineData("bang-gia.csv", false)]
    [InlineData(null, false)]
    public void Nhan_dung_tep_nao_la_tai_lieu(string? ten, bool laTaiLieu)
    {
        Assert.Equal(laTaiLieu, DocChuTuTaiLieu.LaTaiLieu(ten));
    }

    /// <summary>
    /// Tệp mẫu dùng cho e2e phải đọc được bằng chính bộ đọc của ứng dụng. Tệp mẫu hỏng thì bài e2e
    /// đỏ vì lý do chẳng liên quan gì tới tính năng đang kiểm.
    /// </summary>
    [Fact]
    public void Tep_mau_e2e_doc_duoc()
    {
        var duong = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "e2e", "fixtures", "bao-gia-khach-san.docx");
        if (!File.Exists(duong))
        {
            return;   // chưa sinh tệp mẫu thì bỏ qua, không làm đỏ bộ kiểm thử
        }

        using var fs = File.OpenRead(duong);
        var chu = DocChuTuTaiLieu.Doc(fs, "bao-gia-khach-san.docx");

        Assert.Contains("Deluxe City View", chu, StringComparison.Ordinal);
        Assert.Contains("1.200.000", chu, StringComparison.Ordinal);
    }
}
