using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class RoomClassConfiguration : IEntityTypeConfiguration<RoomClass>
{
    public void Configure(EntityTypeBuilder<RoomClass> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
