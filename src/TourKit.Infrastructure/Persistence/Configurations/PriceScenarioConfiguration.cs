using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PriceScenarioConfiguration : IEntityTypeConfiguration<PriceScenario>
{
    public void Configure(EntityTypeBuilder<PriceScenario> builder)
    {
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);

        // Index bắt đầu bằng TenantId (conventions §5).
        builder.HasIndex(x => new { x.TenantId, x.TourTemplateId });
    }
}
