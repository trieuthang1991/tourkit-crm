using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ReceiptApprovalConfiguration : IEntityTypeConfiguration<ReceiptApproval>
{
    public void Configure(EntityTypeBuilder<ReceiptApproval> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.ReceiptVoucherId });
    }
}
