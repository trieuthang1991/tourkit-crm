using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ProviderSubject).IsRequired().HasMaxLength(512);
        builder.Property(x => x.ProviderEmail).IsRequired().HasMaxLength(256);

        builder.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
