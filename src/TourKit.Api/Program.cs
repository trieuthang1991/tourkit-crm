using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourKit.Api.Auth;
using TourKit.Api.Catalog;
using TourKit.Api.Customers;
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
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await TourKit.Api.Authz.PermissionSeeder.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // sau Authentication để đọc được claim
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapTourTemplateEndpoints();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
