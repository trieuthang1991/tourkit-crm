using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Group).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();       // global unique
    }
}
