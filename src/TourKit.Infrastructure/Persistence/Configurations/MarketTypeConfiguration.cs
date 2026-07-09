using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class MarketTypeConfiguration : IEntityTypeConfiguration<MarketType>
{
    public void Configure(EntityTypeBuilder<MarketType> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        // Index bắt đầu bằng TenantId (conventions §5).
        builder.HasIndex(x => new { x.TenantId, x.ParentId });
    }
}
