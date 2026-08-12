using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class SalesOpportunityConfiguration : IEntityTypeConfiguration<SalesOpportunity>
{
    public void Configure(EntityTypeBuilder<SalesOpportunity> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactPhone).HasMaxLength(32);
        builder.Property(x => x.ContactEmail).HasMaxLength(256);
        builder.Property(x => x.ContactAddress).HasMaxLength(300);
        builder.Property(x => x.CancelNote).HasMaxLength(1000);

        // Tiền để precision rõ ràng như Order, không để numeric thả nổi.
        builder.Property(x => x.PriceAdult).HasPrecision(18, 2);
        builder.Property(x => x.PriceChild).HasPrecision(18, 2);
        builder.Property(x => x.PriceChildSmall).HasPrecision(18, 2);
        builder.Property(x => x.PriceBaby).HasPrecision(18, 2);

        // Giá trị phễu KHÔNG có cột: OpportunityMath.GiaTriSelector là biểu thức dịch được sang SQL
        // nên EF tự cộng ở CSDL. Thêm cột sinh sẵn ở đây chỉ tạo ra bản sao thứ hai của công thức.

        // Mã cơ hội KHÔNG tái dùng sau khi xoá — đây là chứng từ, không phải danh mục. Nên index
        // duy nhất ở đây KHÔNG kèm điều kiện "WHERE NOT IsDeleted" như bên danh mục.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        // Bốn lối vào màn Cơ hội, mỗi lối một index: cột phễu (kanban), người tạo, khách, và ngày
        // tạo (báo cáo theo kỳ luôn lọc khoảng thời gian trước rồi mới gom).
        builder.HasIndex(x => new { x.TenantId, x.StageCode });
        builder.HasIndex(x => new { x.TenantId, x.CreatedByUserId });
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });

        builder.HasMany(x => x.Assignees)
            .WithOne()
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SalesOpportunityAssigneeConfiguration : IEntityTypeConfiguration<SalesOpportunityAssignee>
{
    public void Configure(EntityTypeBuilder<SalesOpportunityAssignee> builder)
    {
        // Lối tra QUAN TRỌNG NHẤT là chiều ngược: "cơ hội của tôi". Chính chiều này là thứ hệ cũ
        // không làm được vì nhét id vào cột chuỗi.
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.IsFollower });
        builder.HasIndex(x => new { x.TenantId, x.OpportunityId });

        // Một người chỉ gắn vào một cơ hội một lần cho mỗi vai — bấm hai lần không đẻ ra dòng thừa
        // rồi làm mọi phép đếm "mỗi người giữ bao nhiêu cơ hội" sai lệch.
        builder.HasIndex(x => new { x.OpportunityId, x.UserId, x.IsFollower }).IsUnique();
    }
}

public sealed class OpportunityStageConfiguration : IEntityTypeConfiguration<OpportunityStage>
{
    public void Configure(EntityTypeBuilder<OpportunityStage> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Color).HasMaxLength(16);

        // Cột phễu là DANH MỤC: xoá một cột rồi tạo lại đúng mã đó phải được, nên loại trừ dòng đã
        // xoá mềm khỏi ràng buộc duy nhất (cùng luật với các danh mục khác trong kho này).
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("NOT \"IsDeleted\"");
    }
}
