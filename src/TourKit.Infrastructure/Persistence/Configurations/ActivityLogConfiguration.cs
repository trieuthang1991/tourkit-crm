using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(x => x.Action).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(64);

        // Index bắt đầu bằng TenantId (conventions §5) + truy vấn theo entity bị tác động.
        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId });
    }
}
