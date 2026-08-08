using System.Reflection;
using NetArchTest.Rules;
using TourKit.Ai;
using TourKit.Ai.Abstractions;
using TourKit.Ai.OpenAiCompatible;

namespace TourKit.ArchTests;

/// <summary>
/// Ép các ranh giới của cụm AI. Hai bài đầu bảo vệ dữ liệu: lõi chạm được Api thì mã quyền và tenant
/// có thể bị đi vòng, chạm được EF thì công cụ tự viết truy vấn được. Hai bài sau bảo vệ khả năng mở
/// rộng — hệ thống sẽ tích hợp nhiều hãng AI, và ràng buộc bằng thoả thuận miệng thì không giữ được.
/// </summary>
public class AiLayeringTests
{
    private static readonly Assembly Abstractions = typeof(IAiTool).Assembly;
    private static readonly Assembly Core = typeof(AiToolRegistry).Assembly;
    private static readonly Assembly Adapter = typeof(OpenAiCompatibleChatClientProvider).Assembly;

    [Fact]
    public void Loi_khong_phu_thuoc_Api_hay_Infrastructure()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny("TourKit.Api", "TourKit.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    [Fact]
    public void Loi_khong_dung_EF_truc_tiep()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    /// <summary>
    /// Abstractions phải gọn: nó là thứ MỌI adapter tham chiếu, nên mỗi phụ thuộc thêm vào đây là một
    /// thứ adapter OpenAI/Anthropic/FPT.AI sau này buộc phải kéo theo dù không dùng.
    /// </summary>
    [Fact]
    public void Abstractions_khong_phu_thuoc_project_TourKit_nao()
    {
        var result = Types.InAssembly(Abstractions)
            .ShouldNot().HaveDependencyOnAny(
                "TourKit.Api", "TourKit.Ai.", "TourKit.Infrastructure", "TourKit.Application", "TourKit.Shared")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    /// <summary>
    /// Adapter chỉ được thấy Abstractions. Thấy được lõi thì sửa vòng lặp chat sẽ bắt sửa lại MỌI
    /// adapter — đúng thứ mà việc tách project ra để tránh.
    /// </summary>
    [Fact]
    public void Adapter_khong_duoc_thay_loi()
    {
        var result = Types.InAssembly(Adapter)
            .ShouldNot().HaveDependencyOnAny("TourKit.Api", "TourKit.Application", "TourKit.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    /// <summary>SDK của một hãng không được rò sang lõi — lõi phải trung lập với nhà cung cấp.</summary>
    [Fact]
    public void Loi_khong_keo_theo_SDK_cua_hang_nao()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny("OpenAI", "Anthropic", "Azure.AI")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    private static string Fail(TestResult result) =>
        result.FailingTypeNames is null ? "" : string.Join(", ", result.FailingTypeNames);
}
