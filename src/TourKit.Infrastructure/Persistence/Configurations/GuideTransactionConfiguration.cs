using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class GuideTransactionConfiguration : IEntityTypeConfiguration<GuideTransaction>
{
    public void Configure(EntityTypeBuilder<GuideTransaction> builder)
    {
        builder.Property(x => x.Description).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.TourGuideAssignmentId });
    }
}
