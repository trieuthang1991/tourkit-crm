using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourItineraryConfiguration : IEntityTypeConfiguration<TourItinerary>
{
    public void Configure(EntityTypeBuilder<TourItinerary> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Detail).HasMaxLength(4000);
        builder.HasIndex(x => new { x.TenantId, x.TourId, x.DayIndex });
    }
}
