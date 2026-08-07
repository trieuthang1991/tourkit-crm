using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class EntityCommentConfiguration : IEntityTypeConfiguration<EntityComment>
{
    public void Configure(EntityTypeBuilder<EntityComment> builder)
    {
        // Giới hạn khớp ActivityLog để hai bảng ghép được bằng cùng cặp khoá mà không lệch kiểu.
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);

        // Index bắt đầu bằng TenantId (conventions §5). Truy vấn duy nhất của màn là
        // "lấy bình luận của bản ghi này, mới nhất trước" nên CreatedAt nằm luôn trong index.
        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId, x.CreatedAt });
    }
}
