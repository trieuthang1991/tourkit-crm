using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
{
    public void Configure(EntityTypeBuilder<CustomerTag> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Color).HasMaxLength(50);

        // Index bắt đầu bằng TenantId (conventions §5); Name duy nhất theo tenant (Customer.Tag tham chiếu).
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
