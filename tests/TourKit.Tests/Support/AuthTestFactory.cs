using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Auth;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Tests.Support;

/// <summary>WebApplicationFactory dùng InMemory + tiện ích seed tenant/user để test auth.</summary>
public sealed class AuthTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "AuthTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1" &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))).ToList();
            foreach (var d in toRemove)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Tạo tenant + 1 user (mật khẩu đã hash). Trả về slug + email + password để login.</summary>
    public async Task<(string slug, string email, string password)> SeedTenantUserAsync(string slug)
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var tenantCtx = sp.GetRequiredService<AmbientTenantContext>();

        var tenant = new Tenant { Name = slug, Slug = slug };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        tenantCtx.SetTenant(tenant.Id);   // để interceptor gán TenantId cho User
        var email = $"admin@{slug}.com";
        const string password = "P@ssw0rd!";
        db.Users.Add(new User
        {
            Email = email,
            FullName = "Admin",
            PasswordHash = hasher.Hash(password),
        });
        await db.SaveChangesAsync();

        return (slug, email, password);
    }
}
