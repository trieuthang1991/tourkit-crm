using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Api.Auth;
using TourKit.Application.Notifications;
using TourKit.Infrastructure.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Auth;

public sealed class PasswordResetServiceTests
{
    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }

    private static async Task<(AppDbContext Db, PasswordResetService Service, CapturingEmailSender Email)>
        CreateAsync(string? email)
    {
        var tenantContext = new AmbientTenantContext();
        var db = TestDb.Create(tenantContext, $"PasswordReset-{Guid.NewGuid()}");
        var tenant = new Tenant { Name = "Company", Slug = "company" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        if (email is not null)
        {
            tenantContext.SetTenant(tenant.Id);
            db.Users.Add(new User
            {
                Email = email,
                FullName = "Admin",
                PasswordHash = new PasswordHasher().Hash("P@ssw0rd!"),
            });
            await db.SaveChangesAsync();
        }

        var sender = new CapturingEmailSender();
        var service = new PasswordResetService(
            db, new PasswordHasher(), sender, tenantContext,
            NullLogger<PasswordResetService>.Instance, new EphemeralDataProtectionProvider());
        return (db, service, sender);
    }

    [Fact]
    public async Task Existing_normalized_email_sends_exactly_one_reset_link()
    {
        const string email = "admin@company.test";
        var fixture = await CreateAsync(email);
        await using var db = fixture.Db;

        await fixture.Service.SendResetLinkAsync(
            $"  {email.ToUpperInvariant()}  ", token => "/reset?token=" + token);

        var message = Assert.Single(fixture.Email.Sent);
        Assert.Equal(email, message.To);
        Assert.Contains("/reset?token=", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_existing_email_sends_nothing_without_throwing()
    {
        var fixture = await CreateAsync(null);
        await using var db = fixture.Db;

        await fixture.Service.SendResetLinkAsync(
            "missing@company.test", token => "/reset?token=" + token);

        Assert.Empty(fixture.Email.Sent);
    }
}
