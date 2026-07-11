using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.ShortName).HasMaxLength(150);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Hotline).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.Website).HasMaxLength(255);
        builder.Property(x => x.TaxCode).HasMaxLength(50);
        builder.Property(x => x.LegalRepName).HasMaxLength(200);
        builder.Property(x => x.LegalRepTitle).HasMaxLength(150);
        builder.Property(x => x.LicenseNumber).HasMaxLength(100);
        builder.Property(x => x.BankAccount).HasMaxLength(300);

        // Singleton mỗi tenant.
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
