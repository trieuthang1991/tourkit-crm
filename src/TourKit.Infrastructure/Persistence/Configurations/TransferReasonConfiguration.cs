using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TransferReasonConfiguration : IEntityTypeConfiguration<TransferReason>
{
    public void Configure(EntityTypeBuilder<TransferReason> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}
