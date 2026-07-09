using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TourKit.Api.Application;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Api.Commission;
using TourKit.Api.Crm;
using TourKit.Api.Finance;
using TourKit.Api.Marketing;
using TourKit.Api.Middleware;
using TourKit.Api.Providers;
using TourKit.Api.Provisioning;
using TourKit.Api.Reports;
using TourKit.Api.Tenancy;
using TourKit.Application.Common;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Repositories;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (conventions §7) — Serilog. Không Console.WriteLine.
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

// CORS cho SPA (Vite dev mặc định 5173/4173; prod cấu hình qua Cors:Origins).
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:4173"];
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

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
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- CQRS: dispatcher + validation pipeline (FluentValidation) + scan handler (Scrutor). Không dùng MediatR. ---
builder.Services.AddScoped<IDispatcher, Dispatcher>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<TourKit.Application.Customers.Validators.CreateCustomerValidator>();
builder.Services.Scan(scan => scan.FromAssemblyOf<Program>()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime()
    .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime());

// --- Kiến trúc phân tầng: Controller → Service → IRepository<T> → EF ---
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// Auto-register mọi Application service (I<X>Service → <X>Service) — khỏi khai báo tay từng cái.
builder.Services.Scan(scan => scan.FromAssemblyOf<TourKit.Application.Customers.ICustomerService>()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal)))
        .AsImplementedInterfaces().WithScopedLifetime());

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Giữ nguyên tên claim gốc ("sub" thay vì bị remap sang ClaimTypes.NameIdentifier) —
        // CurrentUser/TenantResolutionMiddleware đọc thẳng "sub"/"tenant_id" theo tên phát hành.
        options.MapInboundClaims = false;
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
    if (db.Database.IsRelational())   // InMemory (test) không migrate được; chỉ migrate SQLite/SqlServer.
    {
        await db.Database.MigrateAsync();   // tự tạo/di trú schema khi khởi động (dev dùng được ngay)
    }
    await TourKit.Api.Authz.PermissionSeeder.SeedAsync(db);
    await PlanSeeder.SeedAsync(db);
}

app.UseSerilogRequestLogging();   // log mỗi request (method/path/status/thời gian) có cấu trúc
app.UseMiddleware<ExceptionHandlingMiddleware>();   // thay UseExceptionHandler: map AppException → HTTP + ProblemDetails
app.UseStatusCodePages();

app.UseCors("web");   // trước Authentication để preflight OPTIONS không cần token

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // sau Authentication để đọc được claim
app.UseMiddleware<SubscriptionGuardMiddleware>();  // chặn nếu subscription hết hạn (miễn trừ auth/đăng ký/billing)
app.UseAuthorization();

app.MapControllers();   // Customers (kiến trúc phân tầng — module mẫu)

app.MapAuthEndpoints();
app.MapRegistrationEndpoints();
app.MapTourTemplateEndpoints();
app.MapMarketTypeEndpoints();
app.MapTourAssigneeEndpoints();
app.MapLeadEndpoints();
app.MapCustomerCareEndpoints();
app.MapTourRatingEndpoints();
app.MapDepartureEndpoints();
app.MapBookingEndpoints();
app.MapReceiptEndpoints();
app.MapReceiptApprovalEndpoints();
app.MapPaymentEndpoints();
app.MapReportEndpoints();
app.MapProviderEndpoints();
app.MapOrderCostEndpoints();
app.MapServiceItemEndpoints();
app.MapProviderServiceEndpoints();
app.MapCommissionEndpoints();
app.MapCommissionRuleEndpoints();
app.MapBillingEndpoints();
app.MapMarketingEndpoints();
app.MapVehicleEndpoints();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
