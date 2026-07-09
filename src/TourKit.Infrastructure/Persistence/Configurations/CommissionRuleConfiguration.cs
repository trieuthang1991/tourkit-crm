using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.Property(x => x.Percentage).HasPrecision(18, 2);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc tra rule theo user.
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}
