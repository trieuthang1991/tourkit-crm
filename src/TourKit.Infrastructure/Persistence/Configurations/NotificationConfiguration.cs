using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Message).HasMaxLength(1000);
        builder.Property(x => x.LinkUrl).HasMaxLength(500);

        // Index bắt đầu bằng TenantId (conventions §5): liệt kê thông báo của 1 user, lọc chưa đọc.
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.IsRead });
    }
}
