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

    public override int SaveChanges()
    {
        ApplyTenantAndTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantAndTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantAndTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenant.TenantId;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != _tenant.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Chặn thao tác chéo tenant: entity thuộc {entry.Entity.TenantId}, request là {_tenant.TenantId}.");
                    }

                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình entity theo IEntityTypeConfiguration<T> (mỗi entity 1 file — xem conventions §5).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter: cô lập tenant + ẩn soft-deleted. Một filter/entity nên phải gộp chung.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;

            if (typeof(ITenantEntity).IsAssignableFrom(clr))
            {
                var filter = TenantFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
            else if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var filter = SoftDeleteFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
        }
    }

    private static readonly System.Reflection.MethodInfo TenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private static readonly System.Reflection.MethodInfo SoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildSoftDeleteFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    // Đóng gói tham chiếu tới _tenant.TenantId — EF Core đánh giá lại mỗi truy vấn.
    private Expression<Func<TEntity, bool>> BuildTenantFilter<TEntity>()
        where TEntity : BaseEntity, ITenantEntity
        => e => e.TenantId == _tenant.TenantId && !e.IsDeleted;

    private static Expression<Func<TEntity, bool>> BuildSoftDeleteFilter<TEntity>()
        where TEntity : BaseEntity
        => e => !e.IsDeleted;
}
