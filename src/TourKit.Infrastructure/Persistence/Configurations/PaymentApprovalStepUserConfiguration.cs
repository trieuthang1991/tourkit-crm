using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PaymentApprovalStepUserConfiguration : IEntityTypeConfiguration<PaymentApprovalStepUser>
{
    public void Configure(EntityTypeBuilder<PaymentApprovalStepUser> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.PaymentVoucherId });
        builder.HasIndex(x => new { x.TenantId, x.PaymentApprovalId, x.StepOrder });
    }
}
