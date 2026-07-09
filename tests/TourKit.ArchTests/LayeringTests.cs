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

    private static string Fail(TestResult result) =>
        "Vi phạm: " + string.Join(", ", result.FailingTypeNames ?? []);
}
