using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ServiceBookingConfiguration : IEntityTypeConfiguration<ServiceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceBooking> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.Type });
        builder.HasIndex(x => new { x.TenantId, x.OrderId });
    }
}
