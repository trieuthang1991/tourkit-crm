using Microsoft.Extensions.DependencyInjection;
using TourKit.Ai;
using TourKit.Api.Authz;
using TourKit.Tests.Support;

namespace TourKit.Tests.Ai;

/// <summary>
/// Mã quyền của công cụ là CHUỖI (lõi AI không được tham chiếu tầng Api), nên một chữ gõ sai sẽ tạo ra
/// công cụ mà không vai trò nào thấy — trợ lý im lặng trả lời "tôi không tra được" mãi mãi và không ai
/// biết vì sao. Bài này là chỗ duy nhất đối chiếu được hai bên.
/// </summary>
public class AiPermissionCodeTests(AuthTestFactory factory) : IClassFixture<AuthTestFactory>
{
    [Fact]
    public void Moi_ma_quyen_cua_cong_cu_deu_ton_tai_that()
    {
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<AiToolRegistry>();

        var known = Permissions.All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var unknown = registry.All
            .Where(t => t.RequiredPermission is not null && !known.Contains(t.RequiredPermission))
            .Select(t => $"{t.Function.Name} → {t.RequiredPermission}")
            .ToList();

        Assert.True(unknown.Count == 0, "Mã quyền không tồn tại: " + string.Join(", ", unknown));
    }

    [Fact]
    public void Co_it_nhat_mot_cong_cu_va_khong_cong_cu_nao_trung_ten()
    {
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<AiToolRegistry>();

        Assert.NotEmpty(registry.All);

        var duplicates = registry.All
            .GroupBy(t => t.Function.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, "Tên công cụ bị trùng: " + string.Join(", ", duplicates));
    }

    /// <summary>Mô tả là thứ model đọc để chọn công cụ — bỏ trống là công cụ coi như không tồn tại.</summary>
    [Fact]
    public void Moi_cong_cu_deu_co_mo_ta_tieng_viet_du_dai()
    {
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<AiToolRegistry>();

        foreach (var tool in registry.All)
        {
            Assert.True((tool.Function.Description?.Length ?? 0) >= 40,
                $"Công cụ {tool.Function.Name} có mô tả quá ngắn.");
        }
    }
}
