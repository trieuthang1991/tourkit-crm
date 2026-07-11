using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PaymentAccountConfiguration : IEntityTypeConfiguration<PaymentAccount>
{
    public void Configure(EntityTypeBuilder<PaymentAccount> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.BankName).HasMaxLength(150);
        builder.Property(x => x.AccountNumber).HasMaxLength(50);
        builder.Property(x => x.AccountHolder).HasMaxLength(150);
        builder.Property(x => x.Branch).HasMaxLength(150);
        builder.Property(x => x.TransferNote).HasMaxLength(300);

        // Index bắt đầu bằng TenantId (conventions §5); Name duy nhất theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
