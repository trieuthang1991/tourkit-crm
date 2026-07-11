using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class SurchargeConfiguration : IEntityTypeConfiguration<Surcharge>
{
    public void Configure(EntityTypeBuilder<Surcharge> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.DefaultValue).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public sealed class OrderSurchargeConfiguration : IEntityTypeConfiguration<OrderSurcharge>
{
    public void Configure(EntityTypeBuilder<OrderSurcharge> builder)
    {
        builder.Property(x => x.Description).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        // Index bắt đầu bằng TenantId (conventions §5).
        builder.HasIndex(x => new { x.TenantId, x.OrderId });
    }
}
