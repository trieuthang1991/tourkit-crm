using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        // Index bắt đầu bằng TenantId (conventions §5): MVP chỉ có 1 subscription active/tenant.
        builder.HasIndex(x => x.TenantId);
    }
}
