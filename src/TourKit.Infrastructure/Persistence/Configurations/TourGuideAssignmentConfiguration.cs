using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourGuideAssignmentConfiguration : IEntityTypeConfiguration<TourGuideAssignment>
{
    public void Configure(EntityTypeBuilder<TourGuideAssignment> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.HandoverContent).HasMaxLength(2000);

        // Index bắt đầu bằng TenantId (conventions §5) + TourDepartureId: truy vấn phân công theo chuyến.
        builder.HasIndex(x => new { x.TenantId, x.TourDepartureId });
    }
}
