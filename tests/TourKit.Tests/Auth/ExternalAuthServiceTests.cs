using Microsoft.EntityFrameworkCore;
using TourKit.Application.Auth;
using TourKit.Infrastructure.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;

namespace TourKit.Tests.Auth;

public sealed class ExternalAuthServiceTests
{
    [Fact]
    public async Task Linked_subject_resolves_linked_user_even_if_provider_email_changed()
    {
        await using var fixture = new Fixture();
        var user = await fixture.AddUserAsync("original@example.com");
        await fixture.AddLoginAsync(user, "Google", "google-subject", "original@example.com");

        var outcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity("Google", "google-subject", "changed@example.com", true, "Changed Name"));

        Assert.Equal(ExternalAuthStatus.Authenticated, outcome.Status);
        Assert.Equal(user.Id, outcome.UserId);
        Assert.Null(outcome.Error);
        Assert.Equal(user.TenantId, fixture.Tenant.TenantId);
        var login = await fixture.Db.UserExternalLogins.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("original@example.com", login.ProviderEmail);
    }

    [Fact]
    public async Task New_subject_with_verified_existing_email_creates_one_link()
    {
        await using var fixture = new Fixture();
        var user = await fixture.AddUserAsync("person@example.com");
        var identity = new ExternalIdentity(
            "Google", "new-google-subject", "  PERSON@EXAMPLE.COM  ", true, "Person");

        var first = await fixture.Service.ResolveAsync(identity);
        var second = await fixture.Service.ResolveAsync(identity);

        Assert.Equal(ExternalAuthStatus.Authenticated, first.Status);
        Assert.Equal(user.Id, first.UserId);
        Assert.Equal(ExternalAuthStatus.Authenticated, second.Status);
        Assert.Equal(user.Id, second.UserId);
        var login = await fixture.Db.UserExternalLogins.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(user.TenantId, login.TenantId);
        Assert.Equal(user.Id, login.UserId);
        Assert.Equal("Google", login.Provider);
        Assert.Equal("new-google-subject", login.ProviderSubject);
        Assert.Equal("PERSON@EXAMPLE.COM", login.ProviderEmail);
    }

    [Fact]
    public async Task New_verified_email_returns_needs_onboarding_without_writes()
    {
        await using var fixture = new Fixture();

        var outcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity("Google", "new-subject", "new@example.com", true, "New Person"));

        Assert.Equal(ExternalAuthStatus.NeedsOnboarding, outcome.Status);
        Assert.Null(outcome.UserId);
        Assert.Null(outcome.Error);
        Assert.Empty(await fixture.Db.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await fixture.Db.UserExternalLogins.IgnoreQueryFilters().ToListAsync());
        Assert.False(fixture.Tenant.HasTenant);
    }

    [Fact]
    public async Task Unverified_email_is_rejected_without_writes()
    {
        await using var fixture = new Fixture();
        var user = await fixture.AddUserAsync("person@example.com");
        fixture.Tenant.SetTenant(Guid.Empty);

        var outcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity("Google", "subject", "person@example.com", false, "Person"));

        Assert.Equal(ExternalAuthStatus.Rejected, outcome.Status);
        Assert.Null(outcome.UserId);
        Assert.NotNull(outcome.Error);
        Assert.Single(await fixture.Db.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await fixture.Db.UserExternalLogins.IgnoreQueryFilters().ToListAsync());
        Assert.False(fixture.Tenant.HasTenant);
        Assert.Equal(user.Id, (await fixture.Db.Users.IgnoreQueryFilters().SingleAsync()).Id);
    }

    [Fact]
    public async Task Existing_user_with_different_google_subject_is_rejected()
    {
        await using var fixture = new Fixture();
        var user = await fixture.AddUserAsync("person@example.com");
        await fixture.AddLoginAsync(user, "Google", "existing-subject", "person@example.com");
        fixture.Tenant.SetTenant(Guid.Empty);

        var outcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity("Google", "conflicting-subject", "person@example.com", true, "Person"));

        Assert.Equal(ExternalAuthStatus.Rejected, outcome.Status);
        Assert.Null(outcome.UserId);
        Assert.NotNull(outcome.Error);
        var login = await fixture.Db.UserExternalLogins.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("existing-subject", login.ProviderSubject);
        Assert.False(fixture.Tenant.HasTenant);
    }

    [Fact]
    public async Task Inactive_user_or_deleted_tenant_is_rejected()
    {
        await using var fixture = new Fixture();
        var inactive = await fixture.AddUserAsync("inactive@example.com", isActive: false);
        await fixture.AddLoginAsync(inactive, "Google", "inactive-subject", "inactive@example.com");
        var deletedTenantUser = await fixture.AddUserAsync("deleted-tenant@example.com", tenantIsDeleted: true);
        await fixture.AddLoginAsync(
            deletedTenantUser, "Google", "deleted-tenant-subject", "deleted-tenant@example.com");
        fixture.Tenant.SetTenant(Guid.Empty);

        var inactiveOutcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity("Google", "inactive-subject", "inactive@example.com", true, "Inactive"));
        var deletedTenantOutcome = await fixture.Service.ResolveAsync(
            new ExternalIdentity(
                "Google", "deleted-tenant-subject", "deleted-tenant@example.com", true, "Deleted Tenant"));

        Assert.Equal(ExternalAuthStatus.Rejected, inactiveOutcome.Status);
        Assert.Null(inactiveOutcome.UserId);
        Assert.NotNull(inactiveOutcome.Error);
        Assert.Equal(ExternalAuthStatus.Rejected, deletedTenantOutcome.Status);
        Assert.Null(deletedTenantOutcome.UserId);
        Assert.NotNull(deletedTenantOutcome.Error);
        Assert.Equal(2, await fixture.Db.UserExternalLogins.IgnoreQueryFilters().CountAsync());
        Assert.False(fixture.Tenant.HasTenant);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private int _tenantNumber;

        public Fixture()
        {
            Tenant = new AmbientTenantContext();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ExternalAuth-{Guid.NewGuid()}")
                .Options;
            Db = new AppDbContext(options, Tenant);
            Identities = new UserIdentityStore(Db);
            Service = new ExternalAuthService(Identities, Tenant);
        }

        public AppDbContext Db { get; }
        public AmbientTenantContext Tenant { get; }
        public UserIdentityStore Identities { get; }
        public ExternalAuthService Service { get; }

        public async Task<User> AddUserAsync(
            string email,
            bool isActive = true,
            bool tenantIsDeleted = false)
        {
            var suffix = ++_tenantNumber;
            var tenant = new Tenant
            {
                Name = $"Tenant {suffix}",
                Slug = $"tenant-{suffix}",
                IsDeleted = tenantIsDeleted,
            };
            Db.Tenants.Add(tenant);
            await Db.SaveChangesAsync();

            Tenant.SetTenant(tenant.Id);
            var user = new User
            {
                Email = email,
                FullName = "Person",
                PasswordHash = "not-used",
                IsActive = isActive,
            };
            Db.Users.Add(user);
            await Db.SaveChangesAsync();
            return user;
        }

        public async Task AddLoginAsync(User user, string provider, string subject, string providerEmail)
        {
            Tenant.SetTenant(user.TenantId);
            Db.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderSubject = subject,
                ProviderEmail = providerEmail,
            });
            await Db.SaveChangesAsync();
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
