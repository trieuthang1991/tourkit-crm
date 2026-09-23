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

        // Nhu cầu khách tự nêu: KHÔNG giới hạn độ dài. Đây là lời khách viết ra, cắt cụt ở một con
        // số tròn trĩnh nào đó là cắt mất đúng đoạn quan trọng nhất.
        builder.Property(x => x.Note);

        // Nguồn chi tiết (utm_*, trang đích, referrer) lưu jsonb trên Postgres — cùng khuôn với
        // Customer.CrmProfileJson và Provider.ProfileJson. Provider khác (test in-memory) bỏ qua.
        builder.Property(x => x.AttributionJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssignedToUserId });

        // Con đếm của vòng chia số đếm lead theo chiến dịch, và nó chạy MỖI LẦN có một lead về từ
        // form thu lead. Không có chỉ mục này thì mỗi lead là một lần quét bảng Lead — đúng lúc
        // đang có đợt quảng cáo đổ về là lúc bảng đó lớn nhất.
        builder.HasIndex(x => new { x.TenantId, x.CampaignId });
    }
}
