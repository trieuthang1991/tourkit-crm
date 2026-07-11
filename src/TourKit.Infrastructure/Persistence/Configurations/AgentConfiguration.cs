using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactPerson).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(300);

        builder.HasIndex(x => new { x.TenantId, x.Code });
    }
}

public sealed class AgentQuoteRequestConfiguration : IEntityTypeConfiguration<AgentQuoteRequest>
{
    public void Configure(EntityTypeBuilder<AgentQuoteRequest> builder)
    {
        builder.Property(x => x.ProductName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SpecialRequests).HasMaxLength(2000);
        builder.Property(x => x.QuotedNote).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.AgentId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
