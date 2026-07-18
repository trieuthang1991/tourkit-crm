using System.Text;

namespace TourKit.Application.Common;

/// <summary>
/// Dựng nội dung CSV (UTF-8 + BOM để Excel đọc đúng tiếng Việt, escape RFC4180) — dùng cho export server-side
/// TOÀN BỘ dữ liệu (không giới hạn trang), khác export client-side chỉ xuất trang đang tải.
/// </summary>
public static class CsvBuilder
{
    /// <summary>Escape RFC4180: ô chứa " , CR/LF → bọc ngoặc kép, nhân đôi dấu ".</summary>
    public static string EscapeCell(string? value)
    {
        var s = value ?? string.Empty;
        return s.IndexOfAny(['"', ',', '\r', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }

    /// <summary>Build CSV (headers + rows) thành mảng byte UTF-8 kèm BOM (U+FEFF).</summary>
    public static byte[] Build(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        sb.Append('﻿'); // BOM để Excel nhận UTF-8
        sb.AppendLine(string.Join(',', headers.Select(EscapeCell)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(EscapeCell)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
