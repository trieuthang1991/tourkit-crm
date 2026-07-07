using Microsoft.EntityFrameworkCore;
using TourKit.Api.Customers;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// Lỗi trả ProblemDetails (RFC 7807) — conventions §6.
builder.Services.AddProblemDetails();

// Provider DB chọn theo cấu hình: mặc định SQLite (dev, không cần server); đổi "SqlServer" khi lên production.
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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapCustomerEndpoints();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
