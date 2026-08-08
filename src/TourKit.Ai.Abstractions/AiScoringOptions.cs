using System.Globalization;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// LUẬT chấm điểm — tiêu chí, trọng số, thang xếp nhóm. Đọc từ file <c>ai-scoring.json</c>, KHÔNG
/// nằm trong <c>appsettings.json</c>.
///
/// Tách ra vì hai thứ này khác bản chất và khác vòng đời:
///   • <c>appsettings.json</c> là CẤU HÌNH HẠ TẦNG (khoá API, chuỗi kết nối, dùng hãng nào) — khác
///     nhau theo từng máy, chứa bí mật, nên nằm ngoài git.
///   • File này là LUẬT NGHIỆP VỤ — giống nhau ở mọi máy, không có bí mật, và khi ai đó đổi trọng số
///     chấm điểm thì phải xem lại được đổi gì, lúc nào, vì sao. Nên nó NẰM TRONG git.
///
/// Trộn hai thứ vào một file thì hoặc luật cũng biến mất khỏi git theo bí mật, hoặc bí mật bị kéo vào
/// git theo luật. Cả hai đều tệ.
/// </summary>
public sealed class AiScoringOptions
{
    /// <summary>Tên section trong <c>ai-scoring.json</c>.</summary>
    public const string SectionName = "AiScoring";

    /// <summary>Tên file luật, đặt cạnh appsettings.json.</summary>
    public const string FileName = "ai-scoring.json";

    /// <summary>Thang xếp nhóm theo điểm tổng. Bỏ trống = dùng thang mặc định trong mã.</summary>
    public IList<AiScoreBand> Bands { get; } = [];

    /// <summary>
    /// Bộ tiêu chí theo LOẠI BẢN GHI ("Lead", "Customer"). Đánh giá khách hàng và đánh giá cơ hội
    /// nhìn vào những dấu hiệu khác nhau nên mỗi loại một bộ riêng.
    /// </summary>
    public IDictionary<string, AiScoringProfile> Profiles { get; } =
        new Dictionary<string, AiScoringProfile>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bộ tiêu chí của một loại bản ghi, hoặc <c>null</c> nếu chưa khai.</summary>
    public AiScoringProfile? For(string entityName) =>
        Profiles.TryGetValue(entityName, out var profile) && profile.Criteria.Count > 0 ? profile : null;

    /// <summary>Soát luật, trả về danh sách lỗi tiếng Việt (rỗng = hợp lệ).</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var band in Bands)
        {
            if (band.Min is < 0 or > 100 || string.IsNullOrWhiteSpace(band.Label))
            {
                errors.Add($"{SectionName}:Bands có bậc không hợp lệ (Min phải 0–100 và Label không được trống).");
                break;
            }
        }

        // Không có bậc nào phủ điểm 0 thì một bản ghi điểm thấp không rơi vào nhóm nào.
        if (Bands.Count > 0 && !Bands.Any(b => b.Min <= 0))
        {
            errors.Add($"{SectionName}:Bands phải có một bậc bắt đầu từ 0, nếu không điểm thấp sẽ không có nhãn.");
        }

        foreach (var (name, profile) in Profiles)
        {
            ValidateProfile($"{SectionName}:Profiles:{name}", profile, errors);
        }

        return errors;
    }

    /// <summary>
    /// Tổng trọng số PHẢI bằng 100 — lệch đi thì điểm tổng vẫn ra một con số trông bình thường nhưng
    /// không còn nằm trên thang 100, và không ai phát hiện bằng mắt.
    /// </summary>
    private static void ValidateProfile(string path, AiScoringProfile profile, List<string> errors)
    {
        if (profile.Criteria.Count == 0)
        {
            errors.Add($"{path}:Criteria đang rỗng — phải có ít nhất một tiêu chí.");
            return;
        }

        foreach (var c in profile.Criteria)
        {
            if (string.IsNullOrWhiteSpace(c.Key) || string.IsNullOrWhiteSpace(c.Label))
            {
                errors.Add($"{path}:Criteria có tiêu chí thiếu Key hoặc Label.");
                return;
            }

            if (c.Weight is < 1 or > 100)
            {
                errors.Add($"{path}:Criteria \"{c.Key}\" có Weight = {c.Weight.ToString(CultureInfo.InvariantCulture)}, phải trong khoảng 1–100.");
                return;
            }
        }

        var duplicate = profile.Criteria
            .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            errors.Add($"{path}:Criteria có Key trùng nhau: \"{duplicate.Key}\".");
            return;
        }

        var total = profile.Criteria.Sum(c => c.Weight);
        if (total != 100)
        {
            errors.Add($"{path}:Criteria có tổng Weight = {total.ToString(CultureInfo.InvariantCulture)}, phải bằng đúng 100.");
        }
    }
}

/// <summary>Một bậc trong thang xếp nhóm: điểm từ <paramref name="Min"/> trở lên thì mang nhãn này.</summary>
/// <param name="Min">Điểm tối thiểu (0–100).</param>
/// <param name="Label">Nhãn hiện cho người dùng, ví dụ "Nóng".</param>
public sealed record AiScoreBand(int Min = 0, string Label = "");

/// <summary>Bộ tiêu chí chấm điểm cho một loại bản ghi.</summary>
public sealed class AiScoringProfile
{
    /// <summary>Các tiêu chí. Tổng trọng số phải bằng 100.</summary>
    public IList<AiScoreCriterion> Criteria { get; } = [];
}

/// <summary>
/// Một tiêu chí chấm điểm.
///
/// Model chỉ chấm TỪNG tiêu chí 0–100; điểm tổng do hệ thống tính theo trọng số, không phải model tự
/// nghĩ ra một con số. Nhờ vậy điểm giải thích được (thấy rõ mất điểm ở đâu), sửa được (đổi trọng số
/// trong file luật), và thêm tiêu chí mới không phải đụng mã nguồn.
/// </summary>
/// <param name="Key">Mã máy, không dấu — model dùng để trả kết quả về đúng tiêu chí.</param>
/// <param name="Label">Nhãn tiếng Việt hiện cho người dùng.</param>
/// <param name="Weight">Trọng số, tổng các tiêu chí trong một bộ phải bằng 100.</param>
/// <param name="Guide">Mô tả cho model biết điểm cao/thấp nghĩa là gì. Càng cụ thể càng ít lệch.</param>
public sealed record AiScoreCriterion(string Key = "", string Label = "", int Weight = 0, string Guide = "");
