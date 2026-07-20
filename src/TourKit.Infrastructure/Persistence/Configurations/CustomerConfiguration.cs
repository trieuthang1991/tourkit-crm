using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.Tag).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.IdCardNumber).HasMaxLength(50);
        builder.Property(x => x.PassportNumber).HasMaxLength(50);
        builder.Property(x => x.Nationality).HasMaxLength(100);
        builder.Property(x => x.SearchName).HasMaxLength(200);
        builder.Property(x => x.PhoneNormalized).HasMaxLength(32);
        builder.Property(x => x.TempBalance).HasPrecision(18, 2);   // H5: cột tiền tạm ứng phải có precision

        // CRM profile (segment/tag/assignedTo/chiến dịch...) lưu jsonb trên Postgres →
        // query bên trong + đếm cho phễu khách hàng. Provider khác (InMemory test) bỏ qua HasColumnType.
        builder.Property(x => x.CrmProfileJson).HasColumnType("jsonb");

        // Index bắt đầu bằng TenantId (conventions §5): tăng tốc mọi truy vấn đã bị lọc theo tenant.
        builder.HasIndex(x => new { x.TenantId, x.FullName });
        builder.HasIndex(x => new { x.TenantId, x.Phone });          // H2: search/dò trùng theo SĐT
        builder.HasIndex(x => new { x.TenantId, x.Email });          // H2: search/dò trùng theo email
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique(); // H3: mã KH duy nhất (NULL cho phép nhiều)
        builder.HasIndex(x => new { x.TenantId, x.PhoneNormalized }); // search/dò trùng SĐT chuẩn hoá (GIN SearchName thêm ở migration)
    }
}
