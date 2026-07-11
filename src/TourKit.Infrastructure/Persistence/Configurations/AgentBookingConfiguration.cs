using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class AgentBookingConfiguration : IEntityTypeConfiguration<AgentBooking>
{
    public void Configure(EntityTypeBuilder<AgentBooking> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.AgentId });
        builder.HasIndex(x => new { x.TenantId, x.QuoteRequestId });
    }
}

public sealed class AgentPassengerConfiguration : IEntityTypeConfiguration<AgentPassenger>
{
    public void Configure(EntityTypeBuilder<AgentPassenger> builder)
    {
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PassportNo).HasMaxLength(50);
        builder.Property(x => x.Nationality).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.AgentBookingId });
    }
}
