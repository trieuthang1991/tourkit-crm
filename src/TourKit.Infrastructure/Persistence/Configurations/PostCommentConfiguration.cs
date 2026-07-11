using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.Property(x => x.AuthorName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.PostId });
    }
}
