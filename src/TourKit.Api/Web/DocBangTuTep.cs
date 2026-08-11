using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using TourKit.Application.Providers.Import;

namespace TourKit.Api.Web;

/// <summary>
/// Bóc tệp CSV/XLSX thành bảng ô chữ cho tầng Application kiểm.
///
/// Cố ý nằm ở tầng Api: tầng Application không được biết định dạng tệp, nhờ vậy toàn bộ luật kiểm dữ
/// liệu kiểm thử được mà không cần dựng tệp thật.
/// </summary>
public static class DocBangTuTep
{
    /// <summary>Trần số dòng đọc từ một tệp. Bảng giá dài hơn mức này thì nên chia tệp.</summary>
    public const int TranDong = 2000;

    public static bool LaXlsx(string? tenTep) =>
        tenTep is not null && tenTep.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    public static BangNhap Doc(Stream stream, string? tenTep) =>
        LaXlsx(tenTep) ? TuXlsx(stream) : TuCsv(stream);

    /// <summary>
    /// CSV tự bóc, không thêm thư viện: phân cách dấu phẩy, ô bọc nháy kép, nháy kép đôi là ký tự thật,
    /// và xuống dòng NẰM TRONG ô bọc nháy thì thuộc về ô đó.
    /// </summary>
    public static BangNhap TuCsv(Stream stream)
    {
        // detectEncodingFromByteOrderMarks: tệp do chính hệ thống xuất ra có BOM UTF-8; thiếu bước này
        // thì ký tự đầu tiên của tiêu đề dính BOM và cột đầu không bao giờ khớp.
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var noiDung = reader.ReadToEnd();

        var hang = new List<List<string>>();
        var o = new StringBuilder();
        var dong = new List<string>();
        var trongNhay = false;

        for (var i = 0; i < noiDung.Length; i++)
        {
            var c = noiDung[i];

            if (trongNhay)
            {
                if (c == '"')
                {
                    if (i + 1 < noiDung.Length && noiDung[i + 1] == '"')
                    {
                        o.Append('"');
                        i++;
                        continue;
                    }

                    trongNhay = false;
                    continue;
                }

                o.Append(c);
                continue;
            }

            switch (c)
            {
                case '"':
                    trongNhay = true;
                    break;
                case ',':
                    dong.Add(o.ToString());
                    o.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    dong.Add(o.ToString());
                    o.Clear();
                    hang.Add(dong);
                    dong = [];
                    if (hang.Count > TranDong) { return Dung(hang); }

                    break;
                default:
                    o.Append(c);
                    break;
            }
        }

        if (o.Length > 0 || dong.Count > 0)
        {
            dong.Add(o.ToString());
            hang.Add(dong);
        }

        return Dung(hang);
    }

    public static BangNhap TuXlsx(Stream stream)
    {
        // OpenXml cần stream tua lại được; IFormFile cho stream tuần tự nên chép sang bộ nhớ trước.
        using var bo = new MemoryStream();
        stream.CopyTo(bo);
        bo.Position = 0;

        using var doc = SpreadsheetDocument.Open(bo, false);
        var phan = doc.WorkbookPart;
        var sheet = phan?.WorksheetParts.FirstOrDefault();
        if (phan is null || sheet is null)
        {
            return new BangNhap([], []);
        }

        var chuoiChung = phan.SharedStringTablePart?.SharedStringTable;
        var hang = new List<List<string>>();

        var trang = sheet.Worksheet;
        if (trang is null)
        {
            return new BangNhap([], []);
        }

        foreach (var r in trang.Descendants<Row>())
        {
            var dong = new List<string>();
            var cot = 0;

            foreach (var c in r.Elements<Cell>())
            {
                // Ô TRỐNG không sinh phần tử <c>: nhảy thẳng từ cột A sang C. Không bù lại thì mọi ô
                // phía sau lệch sang trái một cột và cả dòng đọc sai mà không có lỗi nào.
                var viTri = ViTriCot(c.CellReference?.Value);
                while (cot < viTri)
                {
                    dong.Add("");
                    cot++;
                }

                dong.Add(GiaTri(c, chuoiChung));
                cot++;
            }

            hang.Add(dong);
            if (hang.Count > TranDong)
            {
                break;
            }
        }

        return Dung(hang);
    }

    private static BangNhap Dung(List<List<string>> hang) =>
        hang.Count == 0
            ? new BangNhap([], [])
            : new BangNhap(hang[0], hang.Skip(1).Select(d => (IReadOnlyList<string>)d).ToList());

    /// <summary>"C5" → 2. Chỉ lấy phần chữ cái đầu tham chiếu ô.</summary>
    private static int ViTriCot(string? thamChieu)
    {
        if (string.IsNullOrEmpty(thamChieu))
        {
            return 0;
        }

        var n = 0;
        foreach (var c in thamChieu)
        {
            if (!char.IsLetter(c))
            {
                break;
            }

            n = (n * 26) + (char.ToUpperInvariant(c) - 'A' + 1);
        }

        return n - 1;
    }

    private static string GiaTri(Cell c, SharedStringTable? chuoiChung)
    {
        var v = c.CellValue?.InnerText ?? "";

        // Excel lưu chuỗi trong BẢNG CHUNG và ô chỉ giữ chỉ số. Đọc thẳng InnerText thì mọi ô chữ
        // biến thành con số vô nghĩa.
        if (c.DataType?.Value == CellValues.SharedString && chuoiChung is not null
            && int.TryParse(v, out var idx) && idx >= 0 && idx < chuoiChung.ChildElements.Count)
        {
            return chuoiChung.ChildElements[idx].InnerText;
        }

        return c.DataType?.Value == CellValues.InlineString ? c.InnerText : v;
    }
}
