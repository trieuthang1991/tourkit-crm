using System.Text.Json;
using System.Text.Json.Serialization;

namespace TourKit.Application.Crm;

/// <summary>
/// Nguồn CHI TIẾT của một khách tiềm năng — tầng mở rộng nằm cạnh cột <c>Lead.Source</c>.
///
/// Vì sao hai tầng:
/// <list type="bullet">
///   <item><c>Lead.Source</c> giữ GIÁ TRỊ CHUẨN lấy từ danh mục /nguon-khach. Nó là chiều để gộp
///   báo cáo, nên phải hữu hạn và ổn định — gõ tự do thì "Facebook", "facebook", "FB" thành ba
///   nguồn và mọi báo cáo theo nguồn vỡ theo.</item>
///   <item>Lớp này giữ phần CHI TIẾT, tuỳ ý, không ràng buộc: utm_source=zns, chiến dịch quảng
///   cáo, trang đích, nơi dẫn tới. Thêm một chiều mới chỉ là thêm một khoá, không phải migration.</item>
/// </list>
///
/// <see cref="Khac"/> để hứng mọi tham số không nằm trong danh sách trên — thà giữ lại một khoá lạ
/// còn hơn vứt đi dữ liệu mà sau này mới biết là cần.
///
/// Bám đúng khuôn <c>CustomerCrmProfile</c> / <c>ProviderProfile</c>: cột thật cho thứ cần lọc và
/// sắp xếp ở SQL, JSON cho phần mềm dẻo. Lọc trong JSON là quét cả bảng.
/// </summary>
public sealed record LeadAttribution
{
    public string? UtmSource { get; init; }      // zns, facebook, google…
    public string? UtmMedium { get; init; }      // sms, cpc, email, organic…
    public string? UtmCampaign { get; init; }    // thu-dong-2026
    public string? UtmContent { get; init; }     // biến thể mẫu quảng cáo
    public string? UtmTerm { get; init; }        // từ khoá trả tiền
    public string? LandingPage { get; init; }    // trang khách rơi vào
    public string? Referrer { get; init; }       // nơi dẫn tới

    /// <summary>Tham số khác, giữ nguyên tên gốc. Rỗng thì không ghi vào JSON.</summary>
    public IReadOnlyDictionary<string, string> Khac { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static LeadAttribution Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new LeadAttribution();
        }

        try
        {
            return JsonSerializer.Deserialize<LeadAttribution>(json, Options) ?? new LeadAttribution();
        }
        catch (JsonException)
        {
            // Dữ liệu cũ hoặc ai đó sửa tay hỏng chuỗi: mất phần chi tiết còn hơn hỏng cả màn hình.
            return new LeadAttribution();
        }
    }

    /// <summary>Serialize; <c>null</c> khi rỗng hoàn toàn — không lưu một khối JSON trống.</summary>
    public string? ToJsonOrNull()
    {
        var rong = UtmSource is null && UtmMedium is null && UtmCampaign is null &&
                   UtmContent is null && UtmTerm is null && LandingPage is null &&
                   Referrer is null && Khac.Count == 0;

        return rong ? null : JsonSerializer.Serialize(this, Options);
    }

    /// <summary>
    /// Dựng từ một chuỗi truy vấn kiểu <c>utm_source=zns&amp;utm_medium=sms</c> — dán nguyên link
    /// chiến dịch vào là tách được, không bắt người dùng điền từng ô.
    ///
    /// Nhận cả URL đầy đủ lẫn mỗi phần query. Tham số lạ rơi vào <see cref="Khac"/>.
    /// </summary>
    public static LeadAttribution TuChuoiTruyVan(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new LeadAttribution();
        }

        var query = raw.Contains('?', StringComparison.Ordinal)
            ? raw[(raw.IndexOf('?', StringComparison.Ordinal) + 1)..]
            : raw;

        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var khac = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=', StringComparison.Ordinal);
            if (i <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(part[..i]).Trim();
            var value = Uri.UnescapeDataString(part[(i + 1)..]).Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            {
                known[key] = value;
            }
            else
            {
                khac[key] = value;
            }
        }

        return new LeadAttribution
        {
            UtmSource = Lay(known, "utm_source"),
            UtmMedium = Lay(known, "utm_medium"),
            UtmCampaign = Lay(known, "utm_campaign"),
            UtmContent = Lay(known, "utm_content"),
            UtmTerm = Lay(known, "utm_term"),
            Khac = khac,
        };

        static string? Lay(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) ? v : null;
    }
}
