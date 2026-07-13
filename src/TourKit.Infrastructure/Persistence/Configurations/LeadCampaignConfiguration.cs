using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class LeadCampaignConfiguration : IEntityTypeConfiguration<LeadCampaign>
{
    public void Configure(EntityTypeBuilder<LeadCampaign> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}
