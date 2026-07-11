using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Code).HasMaxLength(50);

        // Index bắt đầu bằng TenantId (conventions §5); Name duy nhất theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
