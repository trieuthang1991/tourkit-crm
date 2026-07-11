using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerTypeConfiguration : IEntityTypeConfiguration<CustomerType>
{
    public void Configure(EntityTypeBuilder<CustomerType> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

        // Index bắt đầu bằng TenantId (conventions §5); Code duy nhất theo tenant để tra Customer.CustomerType.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
