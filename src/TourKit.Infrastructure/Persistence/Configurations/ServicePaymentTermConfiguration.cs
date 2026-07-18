using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ServicePaymentTermConfiguration : IEntityTypeConfiguration<ServicePaymentTerm>
{
    public void Configure(EntityTypeBuilder<ServicePaymentTerm> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(500);

        // Index bắt đầu bằng TenantId (conventions §5): lịch của một booking + cảnh báo đến hạn/quá hạn.
        builder.HasIndex(x => new { x.TenantId, x.ServiceBookingId });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.DueDate });
    }
}
