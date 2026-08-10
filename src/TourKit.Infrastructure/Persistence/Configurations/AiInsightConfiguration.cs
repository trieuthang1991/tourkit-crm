using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class AiInsightConfiguration : IEntityTypeConfiguration<AiInsight>
{
    public void Configure(EntityTypeBuilder<AiInsight> builder)
    {
        // Giới hạn khớp EntityComment/ActivityLog để ba bảng ghép được bằng cùng cặp khoá.
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Band).HasMaxLength(64);

        // Index bắt đầu bằng TenantId (conventions §5). Truy vấn của màn hình luôn là "kết quả loại
        // này của bản ghi này, mới nhất trước", nên Kind và CreatedAt nằm luôn trong index.
        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId, x.Kind, x.CreatedAt });
    }
}
