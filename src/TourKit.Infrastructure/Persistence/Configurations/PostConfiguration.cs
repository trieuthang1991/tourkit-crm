using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PostCategoryConfiguration : IEntityTypeConfiguration<PostCategory>
{
    public void Configure(EntityTypeBuilder<PostCategory> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
    }
}

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Summary).HasMaxLength(500);

        // Index bắt đầu bằng TenantId (conventions §5); Slug duy nhất theo tenant (URL bài viết).
        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
