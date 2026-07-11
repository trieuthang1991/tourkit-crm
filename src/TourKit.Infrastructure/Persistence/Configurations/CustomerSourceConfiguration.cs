using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerSourceConfiguration : IEntityTypeConfiguration<CustomerSource>
{
    public void Configure(EntityTypeBuilder<CustomerSource> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

        // Index bắt đầu bằng TenantId (conventions §5); Name duy nhất theo tenant (Customer.Source tham chiếu).
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
