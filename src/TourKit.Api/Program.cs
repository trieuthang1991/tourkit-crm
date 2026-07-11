using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Middleware;
using TourKit.Api.Provisioning;
using TourKit.Api.Tenancy;
using TourKit.Application.Common;
using TourKit.Application.Reports;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Repositories;
using TourKit.Infrastructure.Reports;
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

// --- Email (conventions §8): dev ghi log; prod dùng SMTP khi Email:Provider=Smtp (giống IFileStorage) ---
builder.Services.Configure<TourKit.Infrastructure.Notifications.EmailOptions>(
    builder.Configuration.GetSection(TourKit.Infrastructure.Notifications.EmailOptions.SectionName));
if (string.Equals(builder.Configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<TourKit.Application.Notifications.IEmailSender, TourKit.Infrastructure.Notifications.SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<TourKit.Application.Notifications.IEmailSender, TourKit.Infrastructure.Notifications.LogEmailSender>();
}

// --- FluentValidation: quét validator ở tầng Application ---
builder.Services.AddValidatorsFromAssemblyContaining<TourKit.Application.Customers.Validators.CreateCustomerValidator>();

// --- Kiến trúc phân tầng: Controller → Service → IRepository<T> → EF ---
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// Repo riêng cho query phức tạp/nhiều bảng (báo cáo GROUP BY) — không dịch được bằng IRepository<T> generic.
builder.Services.AddScoped<IReportQueries, ReportQueries>();
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

app.MapControllers();   // Customers, Providers, Crm (kiến trúc phân tầng)


app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
