using System.Globalization;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// Cấu hình AI của toàn hệ thống, đọc từ section <c>Ai</c> trong appsettings.
///
/// Hình dạng cố ý tách làm hai bảng: <see cref="Providers"/> khai báo NHÀ CUNG CẤP (khoá, địa chỉ,
/// năng lực), <see cref="Features"/> khai báo TÍNH NĂNG dùng nhà cung cấp nào với model nào. Đổi model
/// cho một tính năng là sửa một dòng; đổi hẳn nhà cung cấp cho mọi tính năng là sửa một chỗ.
///
/// KHÔNG để khoá thật trong appsettings.json — file đó nằm trong git. Đặt qua user-secrets khi chạy
/// máy cá nhân (<c>dotnet user-secrets set "Ai:Providers:deepseek:ApiKey" "sk-..."</c>) hoặc biến môi
/// trường khi chạy thật (<c>Ai__Providers__deepseek__ApiKey</c>).
/// </summary>
public sealed class AiOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Ai";

    /// <summary>
    /// Công tắc tổng. Tắt thì mọi tính năng AI im lặng ngừng hoạt động và hệ thống chạy bình thường —
    /// AI là lớp phụ trợ, không được nằm trên đường đi chính của bất kỳ nghiệp vụ nào.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Nhà cung cấp theo khoá tự đặt ("deepseek", "claude", "fptai", "log").</summary>
    public IDictionary<string, AiProviderOptions> Providers { get; } =
        new Dictionary<string, AiProviderOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tính năng theo tên trong <see cref="AiFeatures.All"/>.</summary>
    public IDictionary<string, AiFeatureOptions> Features { get; } =
        new Dictionary<string, AiFeatureOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Hạn mức dùng chung cho mọi tính năng.</summary>
    public AiLimitOptions Limits { get; } = new();

    /// <summary>
    /// Tra cấu hình một tính năng. Trả về <c>null</c> khi công tắc tổng tắt, tính năng tắt, tính năng
    /// chưa khai báo, hoặc nhà cung cấp nó trỏ tới không tồn tại — nơi gọi chỉ cần kiểm tra null là đủ
    /// để rẽ sang đường lui.
    /// </summary>
    public AiFeatureResolution? Resolve(string feature)
    {
        if (!Enabled
            || !Features.TryGetValue(feature, out var settings)
            || !settings.Enabled
            || !Providers.TryGetValue(settings.Provider, out var provider))
        {
            return null;
        }

        return new AiFeatureResolution(feature, settings.Provider, provider, settings);
    }

    /// <summary>
    /// Soát toàn bộ cấu hình, trả về danh sách lỗi bằng tiếng Việt (rỗng = hợp lệ).
    ///
    /// Soát cả tính năng đang TẮT: một tính năng tắt vì gõ sai tên nhà cung cấp trông y hệt một tính
    /// năng tắt có chủ ý, và sẽ không ai phát hiện cho tới lúc bật lên giữa giờ làm việc.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var (name, provider) in Providers)
        {
            ValidateProvider(name, provider, errors);
        }

        foreach (var (name, settings) in Features)
        {
            ValidateFeature(name, settings, errors);
        }

        if (Limits.DailyTokensPerUser < 0)
        {
            errors.Add("Ai:Limits:DailyTokensPerUser không được âm (0 = không giới hạn).");
        }

        if (Limits.RequestsPerMinutePerUser < 0)
        {
            errors.Add("Ai:Limits:RequestsPerMinutePerUser không được âm (0 = không giới hạn).");
        }

        return errors;
    }

    /// <summary>
    /// Tên các nhà cung cấp đang được một tính năng BẬT sử dụng nhưng chưa có khoá.
    ///
    /// Tách khỏi <see cref="Validate"/> vì đây là lỗi của môi trường chứ không phải của file cấu hình:
    /// cùng một appsettings.json là đúng ở máy lập trình (chưa có khoá, AI tắt) và sai ở máy chạy thật.
    /// </summary>
    public IReadOnlyList<string> ProvidersMissingApiKey()
    {
        var missing = new List<string>();

        foreach (var (name, settings) in Features)
        {
            if (!settings.Enabled || !Providers.TryGetValue(settings.Provider, out var provider))
            {
                continue;
            }

            if (!provider.IsFake
                && string.IsNullOrWhiteSpace(provider.ApiKey)
                && !missing.Contains(settings.Provider, StringComparer.OrdinalIgnoreCase))
            {
                missing.Add(settings.Provider);
            }
        }

        return missing;
    }

    /// <summary>
    /// Soát một bộ tiêu chí. Tổng trọng số PHẢI bằng 100 — lệch đi thì điểm tổng vẫn ra một con số
    /// trông bình thường nhưng không còn nằm trên thang 100, và không ai phát hiện bằng mắt.
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

        var duplicate = profile.Criteria.GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
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

    private static void ValidateProvider(string name, AiProviderOptions provider, List<string> errors)
    {
        var path = $"Ai:Providers:{name}";

        if (!AiProviderOptions.KnownKinds.Contains(provider.Kind))
        {
            errors.Add(
                $"{path}:Kind = \"{provider.Kind}\" không hợp lệ. Nhận: {string.Join(", ", AiProviderOptions.KnownKinds)}.");
        }

        if (provider.Capabilities.Count == 0)
        {
            errors.Add($"{path}:Capabilities đang rỗng — phải khai báo ít nhất một năng lực (Chat, Embedding, DocumentRead).");
        }

        foreach (var capability in provider.Capabilities)
        {
            if (!Enum.TryParse<AiCapability>(capability, ignoreCase: true, out _))
            {
                errors.Add($"{path}:Capabilities chứa \"{capability}\" không hợp lệ. Nhận: Chat, Embedding, DocumentRead.");
            }
        }

        // Nhà cung cấp giả chỉ ghi log nên không có địa chỉ để gọi.
        if (!provider.IsFake
            && (!Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            errors.Add($"{path}:BaseUrl phải là địa chỉ http(s) đầy đủ, đang là \"{provider.BaseUrl}\".");
        }

        if (provider.TimeoutSeconds < 1)
        {
            errors.Add($"{path}:TimeoutSeconds phải >= 1, đang là {provider.TimeoutSeconds.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private void ValidateFeature(string name, AiFeatureOptions settings, List<string> errors)
    {
        var path = $"Ai:Features:{name}";

        var required = AiFeatures.Requires(name);
        if (required is null)
        {
            errors.Add($"{path} không phải tính năng đã biết. Tên hợp lệ: {string.Join(", ", AiFeatures.All)}.");
            return;
        }

        // Tính năng chưa dùng tới thì để trống là hợp lệ — chưa tới lượt khai báo.
        if (!settings.Enabled && string.IsNullOrWhiteSpace(settings.Provider))
        {
            return;
        }

        // Bật một tính năng chưa viết thì không có gì hỏng, và đó mới là vấn đề: người đọc cấu hình
        // tưởng nó đang chạy, còn chốt chặn khởi động thì đòi khoá cho một thứ không tồn tại.
        if (settings.Enabled && !AiFeatures.IsImplemented(name))
        {
            errors.Add(
                $"{path} đang bật nhưng \"{AiFeatures.Label(name)}\" CHƯA được triển khai — chưa có mã nguồn nào " +
                "dùng tới nó. Đặt Enabled=false, hoặc nếu bạn vừa viết xong thì thêm tên nó vào AiFeatures.Implemented.");
        }

        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            errors.Add($"{path}:Provider đang trống nhưng tính năng \"{AiFeatures.Label(name)}\" đang bật.");
            return;
        }

        if (!Providers.TryGetValue(settings.Provider, out var provider))
        {
            errors.Add(
                $"{path}:Provider = \"{settings.Provider}\" không có trong Ai:Providers. " +
                $"Đang khai báo: {(Providers.Count == 0 ? "(chưa có)" : string.Join(", ", Providers.Keys))}.");
            return;
        }

        if (!provider.Capabilities.Contains(required.Value.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                $"{path} cần năng lực {required.Value} nhưng nhà cung cấp \"{settings.Provider}\" chỉ khai báo " +
                $"{string.Join(", ", provider.Capabilities)}.");
        }

        // Hãng đã khai danh mục thì model phải nằm trong đó — bắt lỗi gõ sai ngay lúc khởi động thay
        // vì để người dùng bấm nút rồi nhận về một lỗi khó hiểu của hãng.
        if (provider.Models.Count > 0
            && !string.IsNullOrWhiteSpace(settings.Model)
            && !provider.Models.Contains(settings.Model, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                $"{path}:Model = \"{settings.Model}\" không có trong danh mục model của \"{settings.Provider}\". " +
                $"Đang khai: {string.Join(", ", provider.Models)}.");
        }

        // OCR gọi một dịch vụ REST cố định, không có "model" để chọn.
        if (required.Value != AiCapability.DocumentRead && string.IsNullOrWhiteSpace(settings.Model))
        {
            errors.Add($"{path}:Model đang trống — phải nêu rõ model, không có giá trị mặc định ngầm.");
        }

        if (settings.MaxOutputTokens < 1)
        {
            errors.Add($"{path}:MaxOutputTokens phải >= 1, đang là {settings.MaxOutputTokens.ToString(CultureInfo.InvariantCulture)}.");
        }

        // Một model gọi công cụ không ngừng có thể đốt hết hạn mức tháng trong một buổi.
        if (settings.MaxToolRounds is < 1 or > 10)
        {
            errors.Add($"{path}:MaxToolRounds phải trong khoảng 1–10, đang là {settings.MaxToolRounds.ToString(CultureInfo.InvariantCulture)}.");
        }

        foreach (var band in settings.Bands)
        {
            if (band.Min is < 0 or > 100 || string.IsNullOrWhiteSpace(band.Label))
            {
                errors.Add($"{path}:Bands có bậc không hợp lệ (Min phải 0–100 và Label không được trống).");
                break;
            }
        }

        // Không có bậc nào phủ điểm 0 thì một bản ghi điểm thấp sẽ không có nhãn nào để hiện.
        if (settings.Bands.Count > 0 && !settings.Bands.Any(b => b.Min <= 0))
        {
            errors.Add($"{path}:Bands phải có một bậc bắt đầu từ 0, nếu không điểm thấp sẽ không rơi vào nhóm nào.");
        }

        foreach (var (profileName, profile) in settings.Profiles)
        {
            ValidateProfile($"{path}:Profiles:{profileName}", profile, errors);
        }
    }
}

/// <summary>Một nhà cung cấp AI: gọi ở đâu, bằng khoá nào, làm được những việc gì.</summary>
public sealed class AiProviderOptions
{
    /// <summary>Các giá trị <see cref="Kind"/> hợp lệ.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = ["OpenAiCompatible", "Anthropic", "Http", "Log"];

    /// <summary>
    /// Kiểu giao thức, quyết định adapter nào được dựng lên:
    /// <c>OpenAiCompatible</c> (DeepSeek, OpenAI, Groq…), <c>Anthropic</c>, <c>Http</c> (REST riêng như
    /// FPT.AI), <c>Log</c> (giả — chỉ ghi log, dùng khi chưa có khoá).
    /// </summary>
    public string Kind { get; set; } = "OpenAiCompatible";

    /// <summary>Địa chỉ gốc của API. Bỏ trống khi <see cref="Kind"/> = <c>Log</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Khoá API. ĐỂ TRỐNG trong appsettings.json — nạp qua user-secrets hoặc biến môi trường
    /// <c>Ai__Providers__&lt;tên&gt;__ApiKey</c>.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Hết giờ chờ cho một lần gọi.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Năng lực hãng này phục vụ được: <c>Chat</c>, <c>Embedding</c>, <c>DocumentRead</c>.</summary>
    public IList<string> Capabilities { get; } = [];

    /// <summary>
    /// Các mã model của hãng này được phép dùng. Bỏ trống = không giới hạn, gõ mã nào cũng được
    /// (dùng khi hãng vừa ra model mới mà chưa kịp khai).
    ///
    /// Khai ra để gõ sai mã bị bắt ngay lúc khởi động, thay vì lúc người dùng bấm nút rồi nhận về một
    /// lỗi khó hiểu của hãng. Nên dùng model nào cho việc gì thì xem docs/ai-config.md — đó là tri
    /// thức cho người đọc, không phải dữ liệu cho máy chạy.
    /// </summary>
    public IList<string> Models { get; } = [];

    /// <summary>Nhà cung cấp giả (chỉ ghi log) — không cần khoá, không cần địa chỉ.</summary>
    public bool IsFake => string.Equals(Kind, "Log", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Một tính năng AI dùng nhà cung cấp nào, model nào, với giới hạn nào.</summary>
public sealed class AiFeatureOptions
{
    /// <summary>Bật/tắt riêng tính năng này (vẫn phải bật cả <see cref="AiOptions.Enabled"/>).</summary>
    public bool Enabled { get; set; }

    /// <summary>Khoá của nhà cung cấp trong <see cref="AiOptions.Providers"/>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Mã model chính xác của hãng, ví dụ <c>deepseek-chat</c>. Không có mặc định ngầm.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Số token tối đa cho câu trả lời.</summary>
    public int MaxOutputTokens { get; set; } = 4000;

    /// <summary>Số vòng gọi công cụ tối đa mỗi lượt hỏi. Chỉ có ý nghĩa với tính năng dùng công cụ.</summary>
    public int MaxToolRounds { get; set; } = 5;

    /// <summary>
    /// Độ ngẫu nhiên. <c>null</c> = KHÔNG gửi tham số này lên — bắt buộc với các model suy luận
    /// (Claude Opus 5, DeepSeek reasoner) vì chúng trả 400 khi nhận temperature.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>Riêng tính năng này chờ lâu hơn/ngắn hơn nhà cung cấp. 0 = theo nhà cung cấp.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Hướng dẫn NGHIỆP VỤ cho model, mỗi phần tử một dòng. Bỏ trống = dùng bản mặc định trong mã.
    ///
    /// Đây là phần người dùng được sửa: tiêu chí chấm điểm, giọng văn, điều cần chú ý. Phần khung —
    /// vai trò và ĐỊNH DẠNG TRẢ VỀ — vẫn nằm trong mã và không sửa được từ cấu hình, vì sửa hỏng
    /// định dạng thì hệ thống không đọc nổi câu trả lời và tính năng chết hẳn.
    /// </summary>
    public IList<string> Instructions { get; } = [];

    /// <summary>
    /// Ngưỡng xếp nhóm theo điểm. Bỏ trống = dùng thang mặc định. Chỉ có ý nghĩa với tính năng chấm điểm.
    /// </summary>
    public IList<AiScoreBand> Bands { get; } = [];

    /// <summary>
    /// Bộ tiêu chí chấm điểm theo LOẠI BẢN GHI ("Lead", "Customer"). Chỉ có ý nghĩa với tính năng
    /// chấm điểm. Đánh giá khách hàng và đánh giá cơ hội nhìn vào những thứ khác nhau, nên mỗi loại
    /// một bộ tiêu chí riêng.
    /// </summary>
    public IDictionary<string, AiScoringProfile> Profiles { get; } =
        new Dictionary<string, AiScoringProfile>(StringComparer.OrdinalIgnoreCase);
}

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
/// trong cấu hình), và thêm tiêu chí mới không phải đụng mã nguồn.
/// </summary>
/// <param name="Key">Mã máy, không dấu — model dùng để trả kết quả về đúng tiêu chí.</param>
/// <param name="Label">Nhãn tiếng Việt hiện cho người dùng.</param>
/// <param name="Weight">Trọng số, tổng các tiêu chí trong một bộ phải bằng 100.</param>
/// <param name="Guide">Mô tả cho model biết điểm cao/thấp nghĩa là gì. Càng cụ thể càng ít lệch.</param>
public sealed record AiScoreCriterion(string Key = "", string Label = "", int Weight = 0, string Guide = "");

/// <summary>Một bậc trong thang xếp nhóm: điểm từ <paramref name="Min"/> trở lên thì mang nhãn này.</summary>
/// <param name="Min">Điểm tối thiểu (0–100).</param>
/// <param name="Label">Nhãn hiện cho người dùng, ví dụ "Nóng".</param>
public sealed record AiScoreBand(int Min = 0, string Label = "");

/// <summary>Hạn mức chống đốt tiền. 0 = không giới hạn.</summary>
public sealed class AiLimitOptions
{
    /// <summary>Tổng token một người được dùng trong một ngày, tính trên mọi tính năng.</summary>
    public int DailyTokensPerUser { get; set; } = 200_000;

    /// <summary>Số lượt hỏi một người được gửi trong một phút.</summary>
    public int RequestsPerMinutePerUser { get; set; } = 20;
}

/// <summary>Cấu hình đã tra xong cho một tính năng — nơi gọi không phải tự ghép Provider với Feature.</summary>
/// <param name="Feature">Tên tính năng trong <see cref="AiFeatures.All"/>.</param>
/// <param name="ProviderName">Khoá nhà cung cấp trong cấu hình.</param>
/// <param name="Provider">Cấu hình nhà cung cấp.</param>
/// <param name="Settings">Cấu hình riêng của tính năng.</param>
public sealed record AiFeatureResolution(
    string Feature,
    string ProviderName,
    AiProviderOptions Provider,
    AiFeatureOptions Settings)
{
    /// <summary>Hết giờ chờ thực tế: tính năng khai báo riêng thì theo tính năng, không thì theo nhà cung cấp.</summary>
    public int TimeoutSeconds => Settings.TimeoutSeconds > 0 ? Settings.TimeoutSeconds : Provider.TimeoutSeconds;
}
