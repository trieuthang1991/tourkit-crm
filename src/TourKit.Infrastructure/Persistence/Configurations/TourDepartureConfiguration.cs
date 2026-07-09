using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourDepartureConfiguration : IEntityTypeConfiguration<TourDeparture>
{
    public void Configure(EntityTypeBuilder<TourDeparture> builder)
    {
        builder.ToTable("TourDepartureFields");

        // Không thể index (TenantId, IsClosed) cùng lúc: TPT tách TenantId sang bảng gốc "Tours"
        // còn IsClosed ở "TourDepartureFields" — EF cấm index bắc ngang 2 bảng không giao nhau
        // (IndexPropertiesMappedToNonOverlappingTables). Lọc theo tenant đã có ở index gốc
        // (TenantId, Kind, Status) trên Tours; index này chỉ tăng tốc lọc IsClosed trong phạm vi departure.
        builder.HasIndex(x => x.IsClosed);
    }
}
