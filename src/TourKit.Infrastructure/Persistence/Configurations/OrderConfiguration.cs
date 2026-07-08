using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.TotalRefund).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedRevenue).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.TourDepartureId });
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
