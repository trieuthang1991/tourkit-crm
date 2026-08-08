using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.Tests.Auth;

public class UserIdentityPersistenceTests
{
    [Theory]
    [InlineData("Postgres", "uniqueidentifier|datetimeoffset|bit")]
    [InlineData("SqlServer", "uuid|timestamp with time zone|boolean")]
    [InlineData("Sqlite", "uuid|timestamp with time zone|boolean")]
    public void Identity_migration_uses_only_provider_native_store_types(
        string provider,
        string forbiddenStoreTypes)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        switch (provider)
        {
            case "Postgres":
                options.UseNpgsql("Host=localhost;Database=tourkit;Username=tourkit;Password=test");
                break;
            case "SqlServer":
                options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TourKit;Trusted_Connection=True");
                break;
            case "Sqlite":
                options.UseSqlite("Data Source=:memory:");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        using var db = new AppDbContext(options.Options, new AmbientTenantContext());
        var migrator = db.GetService<IMigrator>();
        var upgradeScript = migrator.GenerateScript(
            "20260807160904_AddCommentAttachments",
            "20260808044700_AddGlobalUserIdentity");
        var rollbackScript = migrator.GenerateScript(
            "20260808044700_AddGlobalUserIdentity",
            "20260807160904_AddCommentAttachments");
        var script = upgradeScript + rollbackScript;

        Assert.Contains("UserExternalLogins", script, StringComparison.Ordinal);
        Assert.All(
            forbiddenStoreTypes.Split('|'),
            storeType => Assert.DoesNotContain(storeType, script, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(" Admin@Example.Com ", "ADMIN@EXAMPLE.COM")]
    [InlineData("sales@công-ty.vn", "SALES@CÔNG-TY.VN")]
    public void Normalize_trims_and_uses_invariant_uppercase(string input, string expected)
        => Assert.Equal(expected, UserEmail.Normalize(input));

    [Fact]
    public async Task Normalized_email_is_unique_across_tenants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new AmbientTenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options, tenant);
        await db.Database.EnsureCreatedAsync();

        var a = new Tenant { Name = "A", Slug = "a-company" };
        var b = new Tenant { Name = "B", Slug = "b-company" };
        db.Tenants.AddRange(a, b);
        await db.SaveChangesAsync();

        tenant.SetTenant(a.Id);
        db.Users.Add(new User
        {
            Email = "Admin@Example.com",
            FullName = "A",
            PasswordHash = "hash-a"
        });
        await db.SaveChangesAsync();

        tenant.SetTenant(b.Id);
        db.Users.Add(new User
        {
            Email = " admin@example.COM ",
            FullName = "B",
            PasswordHash = "hash-b"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Provider_subject_is_unique_across_tenants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new AmbientTenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options, tenant);
        await db.Database.EnsureCreatedAsync();

        var a = new Tenant { Name = "A", Slug = "a-company" };
        var b = new Tenant { Name = "B", Slug = "b-company" };
        db.Tenants.AddRange(a, b);
        await db.SaveChangesAsync();

        tenant.SetTenant(a.Id);
        var userA = new User { Email = "a@example.com", FullName = "A", PasswordHash = "hash-a" };
        db.Users.Add(userA);
        await db.SaveChangesAsync();
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = userA.Id,
            Provider = "Google",
            ProviderSubject = "google-sub-1",
            ProviderEmail = "a@example.com"
        });
        await db.SaveChangesAsync();

        tenant.SetTenant(b.Id);
        var userB = new User { Email = "b@example.com", FullName = "B", PasswordHash = "hash-b" };
        db.Users.Add(userB);
        await db.SaveChangesAsync();
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = userB.Id,
            Provider = "Google",
            ProviderSubject = "google-sub-1",
            ProviderEmail = "b@example.com"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
