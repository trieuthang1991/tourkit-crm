using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TourKit.Api.Auth;
using TourKit.Application.Auth;
using TourKit.Application.Provisioning;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Provisioning;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;

namespace TourKit.Tests.Provisioning;

public sealed class ProvisioningServiceTests
{
    private sealed class StaleIdentityStore : IUserIdentityStore
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult<User?>(null);
        public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<User?>(null);
        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task<UserExternalLogin?> FindExternalAsync(
            string provider, string subject, CancellationToken ct = default)
            => Task.FromResult<UserExternalLogin?>(null);
        public Task<bool> HasExternalAsync(Guid userId, string provider, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task AddExternalAsync(UserExternalLogin login, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NullPasswordBeforeSaveInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context is { } db)
            {
                var addedUser = db.ChangeTracker.Entries<User>()
                    .First(entry => entry.State == EntityState.Added);
                addedUser.Entity.PasswordHash = null!;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class ThrowBeforeSaveInterceptor : SaveChangesInterceptor
    {
        private readonly DbUpdateException _exception;

        public ThrowBeforeSaveInterceptor(DbUpdateException exception)
        {
            _exception = exception;
        }

        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                throw _exception;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private static DbContextOptions<AppDbContext> Options(
        SqliteConnection connection,
        params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection);
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static RegisterTenantRequest Request(string slug, string email) =>
        new("New company", slug, email, "P@ssw0rd!", "Admin");

    private static ProvisioningService Service(AppDbContext db, AmbientTenantContext tenant) =>
        new(db, tenant, new PasswordHasher(), new StaleIdentityStore());

    [Fact]
    public async Task Relational_unique_email_race_returns_conflict_and_rolls_back_tenant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var tenantContext = new AmbientTenantContext();
        await using var db = new AppDbContext(Options(connection), tenantContext);
        await db.Database.EnsureCreatedAsync();

        var existingTenant = new Tenant { Name = "Existing", Slug = "existing" };
        db.Tenants.Add(existingTenant);
        await db.SaveChangesAsync();
        tenantContext.SetTenant(existingTenant.Id);
        db.Users.Add(new User
        {
            Email = "shared@company.test",
            FullName = "Existing admin",
            PasswordHash = new PasswordHasher().Hash("P@ssw0rd!"),
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var outcome = await Service(db, tenantContext).RegisterAsync(
            Request("race-company", " SHARED@COMPANY.TEST "));

        Assert.Equal(RegistrationError.Conflict, outcome.Error);
        db.ChangeTracker.Clear();
        Assert.False(await db.Tenants.AnyAsync(tenant => tenant.Slug == "race-company"));
    }

    [Fact]
    public async Task Relational_non_unique_write_failure_propagates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var interceptor = new NullPasswordBeforeSaveInterceptor();
        var tenantContext = new AmbientTenantContext();
        await using var db = new AppDbContext(Options(connection, interceptor), tenantContext);
        await db.Database.EnsureCreatedAsync();
        interceptor.Enabled = true;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Service(db, tenantContext).RegisterAsync(Request("broken-write", "new@company.test")));

        var sqlite = Assert.IsType<SqliteException>(error.InnerException);
        Assert.Equal(1299, sqlite.SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task Wrapped_relational_unique_email_failure_returns_conflict()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var sqlite = new SqliteException(
            "SQLite Error 19: 'UNIQUE constraint failed: Users.NormalizedEmail'.",
            19,
            2067);
        var expected = new DbUpdateException(
            "Registration write failed.",
            new InvalidOperationException("Provider failure was wrapped.", sqlite));
        var interceptor = new ThrowBeforeSaveInterceptor(expected);
        var tenantContext = new AmbientTenantContext();
        await using var db = new AppDbContext(Options(connection, interceptor), tenantContext);
        await db.Database.EnsureCreatedAsync();
        interceptor.Enabled = true;

        var outcome = await Service(db, tenantContext).RegisterAsync(
            Request("wrapped-conflict", "wrapped@company.test"));

        Assert.Equal(RegistrationError.Conflict, outcome.Error);
    }

    [Fact]
    public async Task Wrapped_relational_non_unique_failure_propagates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var sqlite = new SqliteException(
            "SQLite Error 19: 'NOT NULL constraint failed: Users.PasswordHash'.",
            19,
            1299);
        var expected = new DbUpdateException(
            "Registration write failed.",
            new InvalidOperationException("Provider failure was wrapped.", sqlite));
        var interceptor = new ThrowBeforeSaveInterceptor(expected);
        var tenantContext = new AmbientTenantContext();
        await using var db = new AppDbContext(Options(connection, interceptor), tenantContext);
        await db.Database.EnsureCreatedAsync();
        interceptor.Enabled = true;

        var actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Service(db, tenantContext).RegisterAsync(
                Request("wrapped-unrelated", "unrelated@company.test")));

        Assert.Same(expected, actual);
    }
}
