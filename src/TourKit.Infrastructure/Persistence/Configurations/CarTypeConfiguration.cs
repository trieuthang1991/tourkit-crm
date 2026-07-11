using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CarTypeConfiguration : IEntityTypeConfiguration<CarType>
{
    public void Configure(EntityTypeBuilder<CarType> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        // Index bắt đầu bằng TenantId (conventions §5); Code (số ghế) duy nhất theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
