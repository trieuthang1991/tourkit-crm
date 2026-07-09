using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourRatingConfiguration : IEntityTypeConfiguration<TourRating>
{
    public void Configure(EntityTypeBuilder<TourRating> builder)
    {
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.CustomerPhone).HasMaxLength(32);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc tra đánh giá theo chuyến.
        builder.HasIndex(x => new { x.TenantId, x.TourDepartureId });
    }
}
