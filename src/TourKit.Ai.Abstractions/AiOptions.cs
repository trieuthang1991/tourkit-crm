using System.Globalization;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// CẤU HÌNH AI — đọc từ section <c>Ai</c> trong appsettings.json.
///
/// Chỉ chứa hai thứ, tách bạch:
///   • <see cref="Providers"/> — CÁCH KẾT NỐI tới một hãng: giao thức, địa chỉ, khoá, hết giờ chờ.
///   • <see cref="Features"/> — TÍNH NĂNG nào dùng hãng nào, model nào.
///
/// Model ghi thẳng MÃ THẬT của hãng (<c>deepseek-chat</c>, <c>deepseek-reasoner</c>), không qua bí
/// danh trung gian: người cấu hình đọc một dòng là biết đang chạy bằng model gì, không phải lần theo
/// một bảng ánh xạ ở chỗ khác.
///
/// LUẬT NGHIỆP VỤ (tiêu chí chấm điểm, trọng số, thang xếp nhóm) KHÔNG nằm ở đây — nó ở
/// <c>ai-scoring.json</c>. Xem <see cref="AiScoringOptions"/> để biết vì sao tách.
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

    /// <summary>Nhà cung cấp theo khoá tự đặt ("deepseek", "claude", "log").</summary>
    public IDictionary<string, AiProviderOptions> Providers { get; } =
        new Dictionary<string, AiProviderOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tính năng theo tên trong <see cref="AiFeatures.All"/>.</summary>
    public IDictionary<string, AiFeatureOptions> Features { get; } =
        new Dictionary<string, AiFeatureOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Hạn mức dùng chung cho mọi tính năng.</summary>
    public AiLimitOptions Limits { get; } = new();

    /// <summary>
    /// Tra cấu hình một tính năng. Trả <c>null</c> khi công tắc tổng tắt, tính năng tắt, tính năng
    /// chưa khai báo, hoặc nhà cung cấp nó trỏ tới không tồn tại — nơi gọi chỉ cần kiểm tra null là
    /// đủ để rẽ sang đường lui.
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

        foreach (var (_, settings) in Features)
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

    private static void ValidateProvider(string name, AiProviderOptions provider, List<string> errors)
    {
        var path = $"Ai:Providers:{name}";

        if (!AiProviderOptions.KnownKinds.Contains(provider.Kind))
        {
            errors.Add(
                $"{path}:Kind = \"{provider.Kind}\" không hợp lệ. Nhận: {string.Join(", ", AiProviderOptions.KnownKinds)}.");
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

        if (!AiFeatures.IsKnown(name))
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

        if (!Providers.ContainsKey(settings.Provider))
        {
            errors.Add(
                $"{path}:Provider = \"{settings.Provider}\" không có trong Ai:Providers. " +
                $"Đang khai: {(Providers.Count == 0 ? "(chưa có)" : string.Join(", ", Providers.Keys))}.");
            return;
        }

        // OCR gọi một dịch vụ REST cố định, không có "model" để chọn.
        if (!string.Equals(name, AiFeatures.DocumentRead, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(settings.Model))
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
    }
}

/// <summary>
/// CÁCH KẾT NỐI tới một hãng AI.
///
/// Cố ý KHÔNG mô tả hãng đó làm được những việc gì: người cấu hình tự chọn hãng và model cho từng
/// tính năng, nên một bảng "năng lực" song song chỉ lặp lại lựa chọn đó và sẽ lệch khỏi thực tế ngay
/// lần đầu hãng ra thêm dịch vụ mới.
/// </summary>
public sealed class AiProviderOptions
{
    /// <summary>Các giá trị <see cref="Kind"/> hợp lệ.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = ["OpenAiCompatible", "Anthropic", "Http", "Log"];

    /// <summary>
    /// Kiểu giao thức, quyết định adapter nào được dựng lên:
    /// <c>OpenAiCompatible</c> (DeepSeek, OpenAI, Groq, Ollama…), <c>Anthropic</c>, <c>Http</c> (REST
    /// riêng như FPT.AI), <c>Log</c> (giả — chỉ ghi log, dùng khi chưa có khoá).
    /// </summary>
    public string Kind { get; set; } = "OpenAiCompatible";

    /// <summary>Địa chỉ gốc của API. Bỏ trống khi <see cref="Kind"/> = <c>Log</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Khoá API của hãng.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Hết giờ chờ cho một lần gọi.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Nhà cung cấp giả (chỉ ghi log) — không cần khoá, không cần địa chỉ.</summary>
    public bool IsFake => string.Equals(Kind, "Log", StringComparison.OrdinalIgnoreCase);
}

/// <summary>TÍNH NĂNG này dùng nhà cung cấp nào, model nào, với giới hạn nào.</summary>
public sealed class AiFeatureOptions
{
    /// <summary>Bật/tắt riêng tính năng này (vẫn phải bật cả <see cref="AiOptions.Enabled"/>).</summary>
    public bool Enabled { get; set; }

    /// <summary>Khoá của nhà cung cấp trong <see cref="AiOptions.Providers"/>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Mã model CHÍNH XÁC của hãng, ví dụ <c>deepseek-chat</c>. Không có mặc định ngầm.</summary>
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
}

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
/// <param name="Provider">Cách kết nối tới hãng.</param>
/// <param name="Settings">Cấu hình riêng của tính năng.</param>
public sealed record AiFeatureResolution(
    string Feature,
    string ProviderName,
    AiProviderOptions Provider,
    AiFeatureOptions Settings)
{
    /// <summary>Mã model gửi cho hãng.</summary>
    public string Model => Settings.Model;

    /// <summary>Hết giờ chờ thực tế: tính năng khai riêng thì theo tính năng, không thì theo nhà cung cấp.</summary>
    public int TimeoutSeconds => Settings.TimeoutSeconds > 0 ? Settings.TimeoutSeconds : Provider.TimeoutSeconds;
}
