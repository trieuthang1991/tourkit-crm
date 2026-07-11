using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Channel).HasConversion<int>();
        builder.Property(x => x.Subject).HasMaxLength(300);
        builder.HasIndex(x => new { x.TenantId, x.Channel });
    }
}
