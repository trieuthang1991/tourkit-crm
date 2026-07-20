using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class FlightTicketIndividualConfiguration : IEntityTypeConfiguration<FlightTicketIndividual>
{
    public void Configure(EntityTypeBuilder<FlightTicketIndividual> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TicketCode).HasMaxLength(50);
        builder.Property(x => x.Pnr).HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.OrderRef).HasMaxLength(100);
        builder.Property(x => x.ProviderRef).HasMaxLength(100);
        builder.Property(x => x.Route).HasMaxLength(200);
        builder.Property(x => x.AssigneeRef).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();   // H3: mã vé lẻ duy nhất
    }
}
