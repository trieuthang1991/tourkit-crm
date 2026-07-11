using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class VehicleAssignmentConfiguration : IEntityTypeConfiguration<VehicleAssignment>
{
    public void Configure(EntityTypeBuilder<VehicleAssignment> builder)
    {
        builder.Property(x => x.DriverName).HasMaxLength(255);
        builder.Property(x => x.DriverPhone).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(1000);

        // Index bắt đầu bằng TenantId (conventions §5) + TourDepartureId: truy vấn phân xe theo chuyến.
        builder.HasIndex(x => new { x.TenantId, x.TourDepartureId });
    }
}
