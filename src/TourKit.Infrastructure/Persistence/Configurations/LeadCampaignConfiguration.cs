using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class LeadCampaignConfiguration : IEntityTypeConfiguration<LeadCampaign>
{
    public void Configure(EntityTypeBuilder<LeadCampaign> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Code).HasMaxLength(32);
        builder.HasIndex(x => new { x.TenantId, x.Name });

        // Mã là thứ form thu lead gửi lên để tra ra chiến dịch, nên phải DUY NHẤT. Lọc theo
        // IsDeleted giống các danh mục khác: xoá rồi tạo lại đúng mã phải được.
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("NOT \"IsDeleted\"");

        // Nhóm chia số lưu jsonb — cùng khuôn Customer.CrmProfileJson / Provider.ProfileJson.
        builder.Property(x => x.AssigneesJson).HasColumnType("jsonb");
    }
}
