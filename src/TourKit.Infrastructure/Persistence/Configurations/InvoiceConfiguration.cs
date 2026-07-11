using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(x => x.Series).HasMaxLength(20);
        builder.Property(x => x.Number).HasMaxLength(20);
        builder.Property(x => x.BuyerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BuyerTaxCode).HasMaxLength(20);
        builder.Property(x => x.BuyerAddress).HasMaxLength(300);
        builder.Property(x => x.Note).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.InvoiceDate });
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.InvoiceId });
    }
}
