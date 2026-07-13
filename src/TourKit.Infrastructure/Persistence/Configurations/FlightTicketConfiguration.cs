using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class FlightTicketConfiguration : IEntityTypeConfiguration<FlightTicket>
{
    public void Configure(EntityTypeBuilder<FlightTicket> builder)
    {
        builder.Property(x => x.Pnr).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MarketRef).HasMaxLength(100);
        builder.Property(x => x.ProviderRef).HasMaxLength(100);
        builder.Property(x => x.TourType).HasMaxLength(50);
        builder.Property(x => x.OrderRef).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        // Hành trình các chặng lưu jsonb (Postgres). Provider khác (InMemory test) bỏ qua HasColumnType.
        builder.Property(x => x.ItineraryJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.TenantId, x.Pnr });
    }
}
