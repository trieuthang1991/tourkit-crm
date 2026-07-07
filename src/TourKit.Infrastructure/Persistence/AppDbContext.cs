using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filter = BuildTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, null);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)filter!);
            }
        }
    }

    private static readonly System.Reflection.MethodInfo BuildTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private Expression<Func<TEntity, bool>> BuildTenantFilter<TEntity>() where TEntity : class, ITenantEntity
    {
        // Đóng gói tham chiếu tới _tenant.TenantId — EF Core đánh giá lại mỗi truy vấn.
        return e => e.TenantId == _tenant.TenantId;
    }
}
