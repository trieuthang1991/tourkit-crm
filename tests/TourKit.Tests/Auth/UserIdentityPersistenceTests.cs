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

    [Fact]
    public void Sqlite_user_rebuild_runs_inside_the_migration_transaction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new AppDbContext(options, new AmbientTenantContext());
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var migrationType = migrationsAssembly.Migrations["20260808044700_AddGlobalUserIdentity"];
        var migration = migrationsAssembly.CreateMigration(migrationType, db.Database.ProviderName!);
        var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();

        AssertTransactionBoundaries(sqlGenerator.Generate(migration.UpOperations, migration.TargetModel));
        AssertTransactionBoundaries(sqlGenerator.Generate(migration.DownOperations, migration.TargetModel));

        static void AssertTransactionBoundaries(IReadOnlyList<MigrationCommand> commands)
        {
            var pragmaCommands = commands
                .Where(command => command.CommandText.Contains("PRAGMA foreign_keys", StringComparison.Ordinal))
                .ToArray();
            var rebuildCommands = commands
                .Where(command =>
                    command.CommandText.Contains("ef_temp_Users", StringComparison.Ordinal)
                    || command.CommandText.Contains("DROP TABLE \"Users\"", StringComparison.Ordinal))
                .ToArray();

            Assert.NotEmpty(pragmaCommands);
            Assert.All(pragmaCommands, command => Assert.True(command.TransactionSuppressed));
            Assert.NotEmpty(rebuildCommands);
            Assert.All(rebuildCommands, command => Assert.False(command.TransactionSuppressed));
        }
    }

    [Fact]
    public async Task Sqlite_identity_migration_roundtrip_preserves_users_indexes_and_foreign_keys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await ExecuteAsync(
            connection,
            """
            PRAGMA foreign_keys = 1;

            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260807160904_AddCommentAttachments', '9.0.11');

            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "CreatedAt" INTEGER NOT NULL,
                "DepartmentId" TEXT NULL,
                "Email" TEXT NOT NULL,
                "FullName" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "IsDeleted" INTEGER NOT NULL,
                "LastLoginAt" INTEGER NULL,
                "PasswordHash" TEXT NOT NULL,
                "PositionId" TEXT NULL,
                "TenantId" TEXT NOT NULL,
                "UpdatedAt" INTEGER NULL
            );

            CREATE UNIQUE INDEX "IX_Users_TenantId_Email" ON "Users" ("TenantId", "Email");

            CREATE TABLE "UserDependents" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                CONSTRAINT "FK_UserDependents_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );

            INSERT INTO "Users" (
                "Id", "CreatedAt", "DepartmentId", "Email", "FullName", "IsActive", "IsDeleted",
                "LastLoginAt", "PasswordHash", "PositionId", "TenantId", "UpdatedAt"
            ) VALUES (
                '11111111-1111-1111-1111-111111111111', 1, NULL, ' user@example.com ', 'User', 1, 0,
                NULL, 'hash', NULL, '22222222-2222-2222-2222-222222222222', NULL
            );

            INSERT INTO "UserDependents" ("Id", "UserId")
            VALUES ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111');
            """);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options, new AmbientTenantContext());
        var migrator = db.GetService<IMigrator>();
        var upgradeScript = migrator.GenerateScript(
            "20260807160904_AddCommentAttachments",
            "20260808044700_AddGlobalUserIdentity");
        var rollbackScript = migrator.GenerateScript(
            "20260808044700_AddGlobalUserIdentity",
            "20260807160904_AddCommentAttachments");

        await ExecuteAsync(connection, upgradeScript);

        Assert.Equal(1L, await ScalarLongAsync(connection, "PRAGMA foreign_keys;"));
        Assert.Equal(1L, await ScalarLongAsync(connection, "SELECT COUNT(*) FROM \"Users\";"));
        Assert.Equal("USER@EXAMPLE.COM", await ScalarStringAsync(
            connection,
            "SELECT \"NormalizedEmail\" FROM \"Users\";"));
        Assert.Equal(
            "Id|CreatedAt|DepartmentId|Email|FullName|IsActive|IsDeleted|LastLoginAt|NormalizedEmail|PasswordHash|PositionId|TenantId|UpdatedAt",
            await ScalarStringAsync(
                connection,
                "SELECT group_concat(name, '|') FROM (SELECT name FROM pragma_table_info('Users') ORDER BY cid);"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_table_info('Users') WHERE name = 'NormalizedEmail';"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_index_list('Users') WHERE name = 'IX_Users_NormalizedEmail';"));
        Assert.Equal(0L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_index_list('Users') WHERE name = 'IX_Users_TenantId_Email';"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_foreign_key_list('UserExternalLogins') WHERE \"table\" = 'Users';"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_foreign_key_list('UserDependents') WHERE \"table\" = 'Users';"));
        Assert.Equal(1L, await ScalarLongAsync(connection, "SELECT COUNT(*) FROM \"UserDependents\";"));

        await ExecuteAsync(connection, rollbackScript);

        Assert.Equal(1L, await ScalarLongAsync(connection, "PRAGMA foreign_keys;"));
        Assert.Equal(1L, await ScalarLongAsync(connection, "SELECT COUNT(*) FROM \"Users\";"));
        Assert.Equal(" user@example.com ", await ScalarStringAsync(connection, "SELECT \"Email\" FROM \"Users\";"));
        Assert.Equal(
            "Id|CreatedAt|DepartmentId|Email|FullName|IsActive|IsDeleted|LastLoginAt|PasswordHash|PositionId|TenantId|UpdatedAt",
            await ScalarStringAsync(
                connection,
                "SELECT group_concat(name, '|') FROM (SELECT name FROM pragma_table_info('Users') ORDER BY cid);"));
        Assert.Equal(0L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_table_info('Users') WHERE name = 'NormalizedEmail';"));
        Assert.Equal(0L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'UserExternalLogins';"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_index_list('Users') WHERE name = 'IX_Users_TenantId_Email';"));
        Assert.Equal(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pragma_foreign_key_list('UserDependents') WHERE \"table\" = 'Users';"));
        Assert.Equal(1L, await ScalarLongAsync(connection, "SELECT COUNT(*) FROM \"UserDependents\";"));

        static async Task ExecuteAsync(SqliteConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        static async Task<long> ScalarLongAsync(SqliteConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)(await command.ExecuteScalarAsync())!;
        }

        static async Task<string> ScalarStringAsync(SqliteConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (string)(await command.ExecuteScalarAsync())!;
        }
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
