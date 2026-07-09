using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ProfitShareConfiguration : IEntityTypeConfiguration<ProfitShare>
{
    public void Configure(EntityTypeBuilder<ProfitShare> builder)
    {
        builder.Property(x => x.Percentage).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.ProfitBase).HasPrecision(18, 2);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc mọi truy vấn đã bị lọc theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.OrderId });
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}
