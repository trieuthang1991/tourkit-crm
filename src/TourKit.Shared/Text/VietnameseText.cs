using System.Globalization;
using System.Text;

namespace TourKit.Shared.Text;

/// <summary>Chuẩn hoá chuỗi tiếng Việt cho SEARCH (bỏ dấu) và SĐT (chỉ số, +84→0).</summary>
public static class VietnameseText
{
    /// <summary>
    /// lower + bỏ dấu tiếng Việt (đ→d) — dùng cho cột <c>SearchName</c> và cho input ô tìm kiếm.
    /// Trả null nếu rỗng. Ví dụ "Nguyễn Văn Ân" → "nguyen van an".
    /// </summary>
    public static string? NormalizeSearch(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        var lower = s.Trim().ToLowerInvariant();
        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;   // bỏ dấu thanh/dấu mũ
            }

            sb.Append(ch == 'đ' ? 'd' : ch);   // đ không tách dấu qua FormD → thay tay
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Chuẩn hoá SĐT VN: bỏ ký tự không phải số; tiền tố quốc tế 84 → 0 (bắt trùng 0901… vs +84901…).
    /// Trả chuỗi rỗng nếu không có số.
    /// </summary>
    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length > 9)
        {
            digits = "0" + digits[2..];
        }

        return digits;
    }
}
