using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Api.Commission;
using TourKit.Api.Crm;
using TourKit.Api.Customers;
using TourKit.Api.Finance;
using TourKit.Api.Marketing;
using TourKit.Api.Providers;
using TourKit.Api.Provisioning;
using TourKit.Api.Reports;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

// --- Tenancy: 1 instance scoped, vừa là ITenantContext (đọc) vừa set được (login/middleware) ---
builder.Services.AddScoped<AmbientTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

// --- DB provider theo cấu hình ---
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseSqlServer(connectionString);
    }
    else
    {
        opt.UseSqlite(connectionString);
    }
});

// --- Auth services ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization(options =>
{
    foreach (var (code, _) in TourKit.Api.Authz.Permissions.All)
    {
        options.AddPolicy(code, policy => policy.RequireClaim("perm", code));
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await TourKit.Api.Authz.PermissionSeeder.SeedAsync(db);
    await PlanSeeder.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // sau Authentication để đọc được claim
app.UseMiddleware<SubscriptionGuardMiddleware>();  // chặn nếu subscription hết hạn (miễn trừ auth/đăng ký/billing)
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapRegistrationEndpoints();
app.MapCustomerEndpoints();
app.MapTourTemplateEndpoints();
app.MapLeadEndpoints();
app.MapDepartureEndpoints();
app.MapBookingEndpoints();
app.MapReceiptEndpoints();
app.MapReportEndpoints();
app.MapProviderEndpoints();
app.MapOrderCostEndpoints();
app.MapCommissionEndpoints();
app.MapBillingEndpoints();
app.MapMarketingEndpoints();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
