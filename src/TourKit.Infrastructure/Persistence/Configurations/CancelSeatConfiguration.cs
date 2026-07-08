using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CancelSeatConfiguration : IEntityTypeConfiguration<CancelSeat>
{
    public void Configure(EntityTypeBuilder<CancelSeat> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundRemain).HasPrecision(18, 2);
        builder.Property(x => x.RefundPercentage).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.TourCustomerId });
        builder.HasIndex(x => new { x.TenantId, x.OrderId });
    }
}
