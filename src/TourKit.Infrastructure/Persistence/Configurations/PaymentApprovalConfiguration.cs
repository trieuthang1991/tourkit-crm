using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PaymentApprovalConfiguration : IEntityTypeConfiguration<PaymentApproval>
{
    public void Configure(EntityTypeBuilder<PaymentApproval> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.PaymentVoucherId });
    }
}
