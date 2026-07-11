using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class ApprovalProcessConfiguration : IEntityTypeConfiguration<ApprovalProcess>
{
    public void Configure(EntityTypeBuilder<ApprovalProcess> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Method).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class ApprovalProcessStepConfiguration : IEntityTypeConfiguration<ApprovalProcessStep>
{
    public void Configure(EntityTypeBuilder<ApprovalProcessStep> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.ApprovalProcessId, x.StepOrder });
    }
}

public sealed class ApprovalProcessStepUserConfiguration : IEntityTypeConfiguration<ApprovalProcessStepUser>
{
    public void Configure(EntityTypeBuilder<ApprovalProcessStepUser> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.ApprovalProcessStepId });
    }
}
