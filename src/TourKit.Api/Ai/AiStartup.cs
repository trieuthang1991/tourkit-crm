using System.Globalization;
using TourKit.Ai;
using TourKit.Ai.Abstractions;
using TourKit.Ai.OpenAiCompatible;

namespace TourKit.Api.Ai;

/// <summary>
/// Nạp và soát section <c>Ai</c> lúc khởi động.
///
/// Soát ở đây chứ không phải lúc gọi vì hai lỗi cấu hình hay gặp nhất — gõ sai tên nhà cung cấp và
/// quên đặt khoá — đều KHÔNG gây lỗi rõ ràng khi chạy: chúng chỉ làm tính năng im lặng không hoạt
/// động. Thà hỏng lúc deploy còn hơn để người dùng bấm nút và không hiểu vì sao không có gì xảy ra.
/// </summary>
public static class AiStartup
{
    /// <summary>Đăng ký <see cref="AiOptions"/> sau khi đã soát; ném ngoại lệ nếu cấu hình sai.</summary>
    public static void AddTourKitAi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var section = builder.Configuration.GetSection(AiOptions.SectionName);
        var options = section.Get<AiOptions>() ?? new AiOptions();

        // Lỗi cấu trúc file: sai ở mọi môi trường, chặn luôn.
        var errors = options.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Section \"Ai\" trong appsettings không hợp lệ:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        }

        if (options.Enabled)
        {
            GuardApiKeys(builder, options);
        }

        builder.Services.Configure<AiOptions>(section);

        AddScoringRules(builder);

        // Lõi + công cụ. Nằm ở TourKit.Ai, không biết hãng nào cả.
        builder.Services.AddTourKitAiCore();

        // ADAPTER: mỗi Kind một cài đặt. Thêm hãng có giao thức riêng (Anthropic, Gemini) = thêm một
        // project rồi thêm đúng một dòng ở đây. "Log" đã được AddTourKitAiCore đăng ký sẵn.
        builder.Services.AddSingleton<IChatClientProvider, OpenAiCompatibleChatClientProvider>();

        builder.Services.AddSingleton<AiChatClientFactory>();
        // Singleton vì bộ đếm hạn mức phải sống qua nhiều request mới có ý nghĩa.
        builder.Services.AddSingleton<AiUsageGuard>();
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions
            .TryAddSingleton(builder.Services, TimeProvider.System);
        builder.Services.AddScoped<AiAssistant>();
        builder.Services.AddScoped<AiRecordSheet>();
        builder.Services.AddScoped<AiReviewer>();
        builder.Services.AddScoped<AiComposer>();
    }

    /// <summary>
    /// Nạp LUẬT chấm điểm từ file riêng.
    ///
    /// Nguồn cấu hình riêng chứ không nhét vào appsettings vì hai file có vòng đời khác nhau:
    /// appsettings.json chứa khoá nên nằm ngoài git và khác nhau theo máy; file luật không có bí mật,
    /// giống nhau ở mọi máy, và phải xem lại được lịch sử ai đổi trọng số chấm điểm lúc nào.
    ///
    /// reloadOnChange: sửa luật là có hiệu lực ngay, không phải khởi động lại — luật nghiệp vụ được
    /// chỉnh thường xuyên hơn hạ tầng nhiều.
    /// </summary>
    private static void AddScoringRules(WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(AiScoringOptions.FileName, optional: true, reloadOnChange: true);

        var section = builder.Configuration.GetSection(AiScoringOptions.SectionName);
        var rules = section.Get<AiScoringOptions>() ?? new AiScoringOptions();

        var errors = rules.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"File {AiScoringOptions.FileName} không hợp lệ:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        }

        builder.Services.Configure<AiScoringOptions>(section);
    }

    private static void GuardApiKeys(WebApplicationBuilder builder, AiOptions options)
    {
        var missing = options.ProvidersMissingApiKey();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Ai:Enabled = true nhưng chưa có khoá cho: {string.Join(", ", missing)}." + Environment.NewLine +
                $"Điền Ai:Providers:{missing[0]}:ApiKey trong src/TourKit.Api/appsettings.json" + Environment.NewLine +
                $"(hoặc biến môi trường Ai__Providers__{missing[0]}__ApiKey)." + Environment.NewLine +
                "Hoặc tạm đổi Provider của tính năng sang \"log\" để chạy không cần khoá.");
        }

        if (builder.Environment.IsDevelopment())
        {
            return;
        }

        // Cùng luật với Redis/Email/ConnectionStrings ở Program.cs: khoá giải "ENC:" nằm trong mã nguồn
        // nên đó là che mắt, không phải mã hoá — không được dùng cho bí mật thật.
        foreach (var (name, provider) in options.Providers)
        {
            if (provider.ApiKey.StartsWith("ENC:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Ai:Providers:{name}:ApiKey vẫn ở dạng ENC: — khoá giải nằm trong mã nguồn nên đây KHÔNG phải " +
                    $"mã hoá. Đặt giá trị thật qua biến môi trường Ai__Providers__{name}__ApiKey.");
            }
        }
    }

    /// <summary>Tóm tắt một dòng cho log khởi động — nhìn là biết tính năng nào đang chạy bằng model gì.</summary>
    public static string Describe(this AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return "AI: tắt (Ai:Enabled = false)";
        }

        var running = AiFeatures.All
            .Select(options.Resolve)
            .OfType<AiFeatureResolution>()
            .Select(r => string.Create(CultureInfo.InvariantCulture, $"{r.Feature}={r.ProviderName}/{r.Model}"))
            .ToList();

        return running.Count == 0
            ? "AI: bật nhưng chưa tính năng nào được kích hoạt"
            : "AI: " + string.Join(", ", running);
    }
}
