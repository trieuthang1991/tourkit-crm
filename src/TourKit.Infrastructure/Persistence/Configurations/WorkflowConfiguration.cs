using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class WorkflowSectionConfiguration : IEntityTypeConfiguration<WorkflowSection>
{
    public void Configure(EntityTypeBuilder<WorkflowSection> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Color).HasMaxLength(50);
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.WorkflowId, x.Sort });
    }
}
