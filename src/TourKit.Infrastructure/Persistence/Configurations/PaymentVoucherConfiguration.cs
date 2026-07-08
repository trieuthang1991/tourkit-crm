using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PaymentVoucherConfiguration : IEntityTypeConfiguration<PaymentVoucher>
{
    public void Configure(EntityTypeBuilder<PaymentVoucher> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Partner).HasMaxLength(500);
        builder.Property(x => x.ReceiverName).HasMaxLength(255);
        builder.Property(x => x.PhoneNumber).HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.OrderId });
        builder.HasIndex(x => new { x.TenantId, x.ProviderId });
    }
}
