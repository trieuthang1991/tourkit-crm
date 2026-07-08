using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourAssigneeConfiguration : IEntityTypeConfiguration<TourAssignee>
{
    public void Configure(EntityTypeBuilder<TourAssignee> builder)
    {
        // Index bắt đầu bằng TenantId (conventions §5).
        builder.HasIndex(x => new { x.TenantId, x.TourId });
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}
