using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class OrderCostConfiguration : IEntityTypeConfiguration<OrderCost>
{
    public void Configure(EntityTypeBuilder<OrderCost> builder)
    {
        builder.Property(x => x.ServiceName).HasMaxLength(250);
        builder.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ActualAmount).HasPrecision(18, 2);
        builder.Property(x => x.Deposit).HasPrecision(18, 2);
        builder.Property(x => x.Surcharge).HasPrecision(18, 2);
        builder.Property(x => x.Vat).HasPrecision(18, 2);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc mọi truy vấn đã bị lọc theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.OrderId });
        builder.HasIndex(x => new { x.TenantId, x.ProviderId });
    }
}
