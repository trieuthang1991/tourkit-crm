using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Note).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);

        // VAT/phụ thu/tỉ giá (P0-3): default DB đảm bảo dòng cũ tương thích ngược
        // (tỉ giá = 1 → không đổi kết quả; VAT/phụ thu = 0 → giữ nguyên thành tiền).
        builder.Property(x => x.ExchangeRate).HasDefaultValue(1m);
        builder.Property(x => x.VatPercent).HasDefaultValue(0m);
        builder.Property(x => x.Surcharge).HasDefaultValue(0m);

        builder.HasIndex(x => new { x.TenantId, x.QuoteId });
    }
}
