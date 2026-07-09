using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerCareConfiguration : IEntityTypeConfiguration<CustomerCare>
{
    public void Configure(EntityTypeBuilder<CustomerCare> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(255);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc tra chăm sóc theo khách hàng.
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
    }
}
