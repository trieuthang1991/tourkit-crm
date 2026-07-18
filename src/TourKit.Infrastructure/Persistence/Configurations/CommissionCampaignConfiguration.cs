using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CommissionCampaignConfiguration : IEntityTypeConfiguration<CommissionCampaign>
{
    public void Configure(EntityTypeBuilder<CommissionCampaign> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
    }
}

public sealed class CommissionCampaignUserConfiguration : IEntityTypeConfiguration<CommissionCampaignUser>
{
    public void Configure(EntityTypeBuilder<CommissionCampaignUser> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.CommissionCampaignId });
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}

public sealed class CommissionTierConfiguration : IEntityTypeConfiguration<CommissionTier>
{
    public void Configure(EntityTypeBuilder<CommissionTier> builder)
    {
        builder.Property(x => x.StartAmount).HasPrecision(18, 2);
        builder.Property(x => x.EndAmount).HasPrecision(18, 2);
        builder.Property(x => x.Percentage).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.TenantId, x.CommissionCampaignId, x.StartAmount });
    }
}
