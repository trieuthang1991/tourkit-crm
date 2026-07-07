using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourTemplateConfiguration : IEntityTypeConfiguration<TourTemplate>
{
    public void Configure(EntityTypeBuilder<TourTemplate> builder)
    {
        builder.ToTable("TourTemplateFields");
        builder.Property(x => x.PriceAdult).HasPrecision(18, 2);
        builder.Property(x => x.PriceChild).HasPrecision(18, 2);
        builder.Property(x => x.PriceChildSmall).HasPrecision(18, 2);
        builder.Property(x => x.PriceBaby).HasPrecision(18, 2);
        builder.Property(x => x.TermsNote).HasMaxLength(4000);
        builder.Property(x => x.TermsNoteEn).HasMaxLength(4000);
    }
}
