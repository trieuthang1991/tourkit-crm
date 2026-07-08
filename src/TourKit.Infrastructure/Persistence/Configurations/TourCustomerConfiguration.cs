using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourCustomerConfiguration : IEntityTypeConfiguration<TourCustomer>
{
    public void Configure(EntityTypeBuilder<TourCustomer> builder)
    {
        builder.Property(x => x.PriceAdult).HasPrecision(18, 2);
        builder.Property(x => x.PriceChild).HasPrecision(18, 2);
        builder.Property(x => x.PriceChildSmall).HasPrecision(18, 2);
        builder.Property(x => x.PriceBaby).HasPrecision(18, 2);

        builder.Property(x => x.Surcharge).HasPrecision(18, 2);
        builder.Property(x => x.ChildSurcharge).HasPrecision(18, 2);
        builder.Property(x => x.ChildSurchargeSmall).HasPrecision(18, 2);
        builder.Property(x => x.BabySurcharge).HasPrecision(18, 2);

        builder.Property(x => x.Discount).HasPrecision(18, 2);
        builder.Property(x => x.ChildDiscount).HasPrecision(18, 2);
        builder.Property(x => x.ChildDiscountSmall).HasPrecision(18, 2);
        builder.Property(x => x.BabyDiscount).HasPrecision(18, 2);

        builder.Property(x => x.Commission).HasPrecision(18, 2);
        builder.Property(x => x.ChildCommission).HasPrecision(18, 2);
        builder.Property(x => x.ChildCommissionSmall).HasPrecision(18, 2);
        builder.Property(x => x.BabyCommission).HasPrecision(18, 2);

        builder.Property(x => x.UpfrontAmount).HasPrecision(18, 2);

        builder.Property(x => x.ReservationCode).HasMaxLength(64);
        builder.Property(x => x.SeatSelected).HasMaxLength(64);

        builder.HasIndex(x => new { x.TenantId, x.OrderId });
        builder.HasIndex(x => new { x.TenantId, x.TourDepartureId });
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
    }
}
