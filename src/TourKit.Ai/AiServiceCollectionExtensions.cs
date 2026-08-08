using Microsoft.Extensions.DependencyInjection;
using TourKit.Ai.Abstractions;
using TourKit.Ai.Tools;

namespace TourKit.Ai;

/// <summary>Đăng ký lõi trợ lý và toàn bộ công cụ vào DI.</summary>
public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký danh mục công cụ + registry. KHÔNG đăng ký client của hãng nào — việc chọn hãng là của
    /// composition root (tầng Api), nơi duy nhất đọc cấu hình.
    /// </summary>
    public static IServiceCollection AddTourKitAiCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Thêm công cụ mới thì thêm đúng một dòng ở đây. Mỗi công cụ tự khai mã quyền của nó.
        services.AddScoped<IAiTool, BusinessOverviewTool>();
        services.AddScoped<IAiTool, KpiSummaryTool>();
        services.AddScoped<IAiTool, TurnoverByBranchTool>();
        services.AddScoped<IAiTool, TurnoverByDepartmentTool>();
        services.AddScoped<IAiTool, MoneyByTourTypeTool>();
        services.AddScoped<IAiTool, TopCustomersTool>();
        services.AddScoped<IAiTool, OrderDebtTool>();
        services.AddScoped<IAiTool, ProviderDebtTool>();
        services.AddScoped<IAiTool, CashFlowTool>();

        services.AddScoped<AiToolRegistry>();

        // Adapter giả luôn có mặt: một máy chưa cấu hình khoá vẫn mở được màn hình trợ lý.
        services.AddSingleton<IChatClientProvider, LogChatClientProvider>();

        return services;
    }
}
