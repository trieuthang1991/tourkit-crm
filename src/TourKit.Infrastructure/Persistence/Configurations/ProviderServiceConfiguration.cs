using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ProviderServiceConfiguration : IEntityTypeConfiguration<ProviderService>
{
    public void Configure(EntityTypeBuilder<ProviderService> builder)
    {
        builder.Property(x => x.PriceName).HasMaxLength(200);
        builder.Property(x => x.ContractPrice).HasPrecision(18, 2);
        builder.Property(x => x.PublicPrice).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(1000);

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc lọc bảng giá theo NCC.
        builder.HasIndex(x => new { x.TenantId, x.ProviderId });
    }
}
