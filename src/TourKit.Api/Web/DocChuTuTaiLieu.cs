using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace TourKit.Api.Web;

/// <summary>
/// Bóc CHỮ từ tài liệu báo giá (PDF/DOCX) để đưa cho AI đọc.
///
/// Chỉ lấy chữ, không cố hiểu bố cục: việc khớp chữ vào từng cột là của model, còn việc kiểm dữ liệu
/// là của lớp kiểm tất định. Chia ba tầng như vậy để mỗi tầng hỏng đều lộ ra ở một chỗ khác nhau.
/// </summary>
public static class DocChuTuTaiLieu
{
    /// <summary>
    /// Trần ký tự gửi cho model. Một bảng giá dài vài chục trang sẽ vượt cửa sổ ngữ cảnh và bị cắt
    /// giữa chừng — cắt CÓ CHỦ Ý ở đây rồi báo cho người dùng vẫn hơn để nhà cung cấp tự cắt ngẫu nhiên.
    /// </summary>
    public const int TranKyTu = 60_000;

    public static bool LaTaiLieu(string? tenTep) =>
        tenTep is not null &&
        (tenTep.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
         || tenTep.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
         || tenTep.EndsWith(".doc", StringComparison.OrdinalIgnoreCase));

    /// <summary>Bóc chữ; trả về chuỗi rỗng nếu tài liệu không có chữ nào đọc được.</summary>
    public static string Doc(Stream stream, string? tenTep)
    {
        // Cả hai bộ đọc đều cần stream tua lại được; IFormFile cho stream tuần tự.
        using var bo = new MemoryStream();
        stream.CopyTo(bo);
        bo.Position = 0;

        var chu = tenTep is not null && tenTep.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? TuPdf(bo)
            : TuDocx(bo);

        return chu.Length > TranKyTu ? chu[..TranKyTu] : chu;
    }

    private static string TuPdf(Stream s)
    {
        using var doc = PdfDocument.Open(s);
        var sb = new StringBuilder();
        foreach (var trang in doc.GetPages())
        {
            sb.AppendLine(trang.Text);
        }

        return sb.ToString();
    }

    /// <summary>
    /// DOCX: lấy chữ theo TỪNG đoạn và TỪNG ô bảng, mỗi thứ một dòng.
    ///
    /// Đọc thẳng InnerText của cả tài liệu thì mọi ô của một bảng dính liền thành một chuỗi không có
    /// ranh giới — mà bảng giá trong file Word gần như luôn là bảng thật. Model sẽ không biết đâu là
    /// hết ô này sang ô kia và ghép giá của dòng này vào tên gói của dòng khác.
    /// </summary>
    private static string TuDocx(Stream s)
    {
        using var doc = WordprocessingDocument.Open(s, false);
        var than = doc.MainDocumentPart?.Document?.Body;
        if (than is null)
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var el in than.Elements())
        {
            switch (el)
            {
                case Table bang:
                    foreach (var hang in bang.Elements<TableRow>())
                    {
                        var o = hang.Elements<TableCell>().Select(c => c.InnerText.Trim());
                        sb.AppendLine(string.Join(" | ", o));
                    }

                    sb.AppendLine();
                    break;

                case Paragraph doan:
                    var t = doan.InnerText.Trim();
                    if (t.Length > 0)
                    {
                        sb.AppendLine(t);
                    }

                    break;
            }
        }

        return sb.ToString();
    }
}
