using System.Reflection;
using NetArchTest.Rules;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.ArchTests;

/// <summary>
/// Ép CHIỀU PHỤ THUỘC bằng test (conventions §10) — fail build nếu ranh giới tầng bị phá.
/// Shared (kernel) → không biết gì; Infrastructure → không biết Api; kernel không dính EF.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Shared = typeof(BaseEntity).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly Application = typeof(TourKit.Application.Customers.ICustomerService).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    /// <summary>
    /// Tầng Api chỉ còn được đụng EF ở ĐÚNG mấy chỗ hạ tầng khởi động dưới đây. Mọi thứ khác phải đi
    /// qua Application/Infrastructure. Danh sách này là "trần nợ": chỉ được rút ngắn, không được nới ra.
    /// </summary>
    private static readonly string[] ApiEfExceptions =
    [
        // Top-level statements của Program.cs (migrate DB lúc khởi động + đăng ký DbContext theo provider).
        "Program",
        // Seeder chạy một lần lúc khởi động: quyền RBAC và catalog gói dịch vụ.
        "TourKit.Api.Authz.PermissionSeeder",
        "TourKit.Api.Billing.PlanSeeder",
        // Middleware chặn tenant hết hạn subscription — đọc thẳng DB trên đường request.
        "TourKit.Api.Billing.SubscriptionGuardMiddleware",
        // Job nền (Hangfire) và seeder dữ liệu mẫu/hiệu năng: DEV-ONLY hoặc chưa tách repo.
        "TourKit.Api.BackgroundJobs.CareReminderJob",
        "TourKit.Api.BackgroundJobs.HoldReleaseJob",
        "TourKit.Api.BackgroundJobs.HoldReminderJob",
        "TourKit.Api.DevData.DemoDataSeeder",
        "TourKit.Api.DevData.PerfDataSeeder",
    ];

    [Fact]
    public void Shared_khong_phu_thuoc_Infrastructure_hay_Api()
    {
        var result = Types.InAssembly(Shared)
            .ShouldNot().HaveDependencyOnAny("TourKit.Infrastructure", "TourKit.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    [Fact]
    public void Infrastructure_khong_phu_thuoc_Api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot().HaveDependencyOn("TourKit.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    [Fact]
    public void Application_khong_phu_thuoc_Infrastructure_Api()
    {
        var r = Types.InAssembly(Application).ShouldNot()
            .HaveDependencyOnAny("TourKit.Infrastructure", "TourKit.Api").GetResult();
        Assert.True(r.IsSuccessful, Fail(r));
    }

    [Fact]
    public void Kernel_Shared_khong_dinh_EntityFrameworkCore()
    {
        var result = Types.InAssembly(Shared)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    [Fact]
    public void Api_khong_dinh_EntityFrameworkCore()
    {
        // Chống tái phát: auth/provisioning từng nằm ở Api và gọi thẳng AppDbContext (33 chỗ). Thêm file
        // mới ở Api mà đụng EF là fail ngay tại đây, chứ không đợi tới lúc review.
        var offenders = (Types.InAssembly(Api)
                .That().HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetTypes() ?? [])
            .Select(t => t.FullName ?? t.Name)
            .Where(name => !ApiEfExceptions.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            "Tầng Api không được dùng EF Core trực tiếp. Vi phạm: " + string.Join(", ", offenders));
    }

    private static string Fail(TestResult result) =>
        "Vi phạm: " + string.Join(", ", result.FailingTypeNames ?? []);
}
