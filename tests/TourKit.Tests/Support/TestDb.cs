using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.Tests.Support;

public static class TestDb
{
    /// <summary>Tạo AppDbContext InMemory. Cùng dbName = cùng "database" (chia sẻ dữ liệu giữa các context).</summary>
    public static AppDbContext Create(ITenantContext tenant, string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options, tenant);
    }
}
