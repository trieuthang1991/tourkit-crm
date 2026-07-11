using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourTransferConfiguration : IEntityTypeConfiguration<TourTransfer>
{
    public void Configure(EntityTypeBuilder<TourTransfer> builder)
    {
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.OrderId });
    }
}
