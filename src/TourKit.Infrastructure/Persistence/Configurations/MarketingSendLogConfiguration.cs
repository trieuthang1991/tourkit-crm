using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class MarketingSendLogConfiguration : IEntityTypeConfiguration<MarketingSendLog>
{
    public void Configure(EntityTypeBuilder<MarketingSendLog> builder)
    {
        builder.Property(x => x.Recipient).IsRequired().HasMaxLength(256);

        builder.HasIndex(x => new { x.TenantId, x.CampaignId });
    }
}
