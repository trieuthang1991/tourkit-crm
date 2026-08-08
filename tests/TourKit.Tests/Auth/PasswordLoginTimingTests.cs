using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TourKit.Api.Auth;
using TourKit.Application.Auth;
using TourKit.Infrastructure.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;

namespace TourKit.Tests.Auth;

public sealed class PasswordLoginTimingTests
{
    [Fact]
    public async Task Api_login_with_unknown_email_still_verifies_a_password_hash()
    {
        var store = new StubIdentityStore();
        var hasher = new TrackingPasswordHasher();
        await using var db = CreateDb(out var tenant);
        var service = new AuthService(db, tenant, store, hasher, new UnusedJwtTokenService(),
            Options.Create(new JwtOptions()));

        var result = await service.LoginAsync(new LoginRequest("missing@example.com", "not-the-password"));

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    [Fact]
    public async Task Cookie_login_with_unknown_email_still_verifies_a_password_hash()
    {
        var store = new StubIdentityStore();
        var hasher = new TrackingPasswordHasher();
        await using var db = CreateDb(out var tenant);
        var service = new CookieAuthService(db, tenant, store, hasher);

        var result = await service.AuthenticateAsync("missing@example.com", "not-the-password");

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    [Fact]
    public async Task Api_login_with_inactive_user_still_verifies_the_stored_hash()
    {
        var hasher = new TrackingPasswordHasher();
        var user = ActiveUser(hasher.Hash("account-specific-password"));
        user.IsActive = false;
        var store = new StubIdentityStore { User = user };
        await using var db = CreateDb(out var tenant);
        var service = new AuthService(db, tenant, store, hasher, new UnusedJwtTokenService(),
            Options.Create(new JwtOptions()));

        var result = await service.LoginAsync(new LoginRequest(user.Email, "account-specific-password"));

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
        Assert.Equal(user.PasswordHash, hasher.LastVerifiedHash);
    }

    [Fact]
    public async Task Cookie_login_with_inactive_user_still_verifies_the_stored_hash()
    {
        var hasher = new TrackingPasswordHasher();
        var user = ActiveUser(hasher.Hash("account-specific-password"));
        user.IsActive = false;
        var store = new StubIdentityStore { User = user };
        await using var db = CreateDb(out var tenant);
        var service = new CookieAuthService(db, tenant, store, hasher);

        var result = await service.AuthenticateAsync(user.Email, "account-specific-password");

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
        Assert.Equal(user.PasswordHash, hasher.LastVerifiedHash);
    }

    [Fact]
    public async Task Api_login_rejects_user_from_deleted_tenant_after_password_verification()
    {
        var hasher = new TrackingPasswordHasher();
        var user = ActiveUser(hasher.Hash("account-specific-password"));
        var store = new StubIdentityStore { User = user, TenantIsActive = false };
        await using var db = CreateDb(out var tenant);
        var service = new AuthService(db, tenant, store, hasher, new UnusedJwtTokenService(),
            Options.Create(new JwtOptions()));

        var result = await service.LoginAsync(new LoginRequest(user.Email, "account-specific-password"));

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    [Fact]
    public async Task Cookie_login_rejects_user_from_deleted_tenant_after_password_verification()
    {
        var hasher = new TrackingPasswordHasher();
        var user = ActiveUser(hasher.Hash("account-specific-password"));
        var store = new StubIdentityStore { User = user, TenantIsActive = false };
        await using var db = CreateDb(out var tenant);
        var service = new CookieAuthService(db, tenant, store, hasher);

        var result = await service.AuthenticateAsync(user.Email, "account-specific-password");

        Assert.Null(result);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    [Fact]
    public async Task Cookie_principal_rejects_inactive_user()
    {
        var user = ActiveUser("unused");
        user.IsActive = false;
        var store = new StubIdentityStore { User = user };
        await using var db = CreateDb(out var tenant);
        var service = new CookieAuthService(db, tenant, store, new TrackingPasswordHasher());

        var principal = await service.CreatePrincipalAsync(user.Id);

        Assert.Null(principal);
    }

    [Fact]
    public async Task Cookie_principal_rejects_user_from_deleted_tenant()
    {
        var user = ActiveUser("unused");
        var store = new StubIdentityStore { User = user, TenantIsActive = false };
        await using var db = CreateDb(out var tenant);
        var service = new CookieAuthService(db, tenant, store, new TrackingPasswordHasher());

        var principal = await service.CreatePrincipalAsync(user.Id);

        Assert.Null(principal);
    }

    private static AppDbContext CreateDb(out AmbientTenantContext tenant)
    {
        tenant = new AmbientTenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"PasswordLoginTiming-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options, tenant);
    }

    private static User ActiveUser(string passwordHash)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "person@example.com",
            FullName = "Person",
            PasswordHash = passwordHash,
        };

    private sealed class TrackingPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher _inner = new();

        public int VerifyCalls { get; private set; }
        public string? LastVerifiedHash { get; private set; }

        public string Hash(string password) => _inner.Hash(password);

        public bool Verify(string hash, string password)
        {
            VerifyCalls++;
            LastVerifiedHash = hash;
            return _inner.Verify(hash, password);
        }
    }

    private sealed class StubIdentityStore : IUserIdentityStore
    {
        public User? User { get; init; }
        public bool TenantIsActive { get; init; } = true;

        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(User);

        public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(User);

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
            => Task.FromResult(User is not null);

        public Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken ct = default)
            => Task.FromResult(TenantIsActive);

        public Task<UserExternalLogin?> FindExternalAsync(
            string provider,
            string subject,
            CancellationToken ct = default)
            => Task.FromResult<UserExternalLogin?>(null);

        public Task<bool> HasExternalAsync(Guid userId, string provider, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task AddExternalAsync(UserExternalLogin login, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class UnusedJwtTokenService : IJwtTokenService
    {
        public string CreateAccessToken(User user, IEnumerable<string> permissions)
            => throw new InvalidOperationException("JWT should not be issued for a rejected login.");

        public string CreateRefreshToken()
            => throw new InvalidOperationException("JWT should not be issued for a rejected login.");

        public DateTimeOffset AccessTokenExpiry()
            => throw new InvalidOperationException("JWT should not be issued for a rejected login.");
    }
}
