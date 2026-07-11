using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TicketFundConfiguration : IEntityTypeConfiguration<TicketFund>
{
    public void Configure(EntityTypeBuilder<TicketFund> builder)
    {
        builder.Property(x => x.TicketCode).HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.OrderId });
    }
}
