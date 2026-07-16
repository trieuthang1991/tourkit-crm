using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class RoomAllotmentConfiguration : IEntityTypeConfiguration<RoomAllotment>
{
    public void Configure(EntityTypeBuilder<RoomAllotment> builder)
    {
        builder.Property(x => x.ProviderRef).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ProjectName).HasMaxLength(200);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.Market).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.ProviderRef, x.Date });
    }
}
