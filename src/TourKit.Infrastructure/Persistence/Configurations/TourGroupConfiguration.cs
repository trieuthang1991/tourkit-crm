using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourGroupConfiguration : IEntityTypeConfiguration<TourGroup>
{
    public void Configure(EntityTypeBuilder<TourGroup> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Code).HasMaxLength(50);

        // Index bắt đầu bằng TenantId (conventions §5); Name duy nhất theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
