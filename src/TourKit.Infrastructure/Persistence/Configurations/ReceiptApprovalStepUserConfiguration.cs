using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ReceiptApprovalStepUserConfiguration : IEntityTypeConfiguration<ReceiptApprovalStepUser>
{
    public void Configure(EntityTypeBuilder<ReceiptApprovalStepUser> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.ReceiptVoucherId });
        builder.HasIndex(x => new { x.TenantId, x.ReceiptApprovalId, x.StepOrder });
    }
}
