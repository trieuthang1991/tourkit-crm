using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Source).HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssignedToUserId });
    }
}
