using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.TaxCode).HasMaxLength(32);
        builder.Property(x => x.ContactPerson).HasMaxLength(200);
        builder.Property(x => x.BankAccount).HasMaxLength(64);
        builder.Property(x => x.BankName).HasMaxLength(200);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc mọi truy vấn đã bị lọc theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Type, x.Status });
    }
}
