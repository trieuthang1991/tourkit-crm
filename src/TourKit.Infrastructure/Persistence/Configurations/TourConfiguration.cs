using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.UseTptMappingStrategy();               // TPT: mỗi type 1 bảng, chia sẻ PK
        builder.ToTable("Tours");

        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TourType).HasMaxLength(50);
        builder.Property(x => x.PickupPlace).HasMaxLength(300);
        builder.Property(x => x.DropoffPlace).HasMaxLength(300);
        builder.Property(x => x.TransportMode).HasMaxLength(100);

        // Index bắt đầu bằng TenantId (conventions §5 / DB §H).
        builder.HasIndex(x => new { x.TenantId, x.Kind, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.Code });
        builder.HasIndex(x => new { x.TenantId, x.DepartureDate });
    }
}
