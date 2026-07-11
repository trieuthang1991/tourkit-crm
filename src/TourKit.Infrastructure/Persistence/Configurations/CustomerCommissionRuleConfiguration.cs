using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerCommissionRuleConfiguration : IEntityTypeConfiguration<CustomerCommissionRule>
{
    public void Configure(EntityTypeBuilder<CustomerCommissionRule> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.CustomerType });
    }
}
