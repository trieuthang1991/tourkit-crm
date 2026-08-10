using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Microsoft.Extensions.Options;
using TourKit.Api.Ai;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Configuration;
using TourKit.Infrastructure.Notifications;
using TourKit.Infrastructure.Storage;
using TourKit.Api.Middleware;
using TourKit.Api.Tenancy;
using TourKit.Application.Auth;
using TourKit.Application.Common;
using TourKit.Application.Provisioning;
using TourKit.Application.Reports;
using TourKit.Caching;
using TourKit.Infrastructure.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Provisioning;
using TourKit.Infrastructure.Repositories;
using TourKit.Infrastructure.Reports;
using TourKit.Infrastructure.Tenancy;
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
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");            // mọi trang cần đăng nhập
    options.Conventions.AllowAnonymousToFolder("/Auth"); // trừ đăng nhập/đăng xuất
    options.Conventions.AllowAnonymousToPage("/Index");  // landing page công khai
    options.Conventions.AllowAnonymousToPage("/Ping");   // trang smoke hạ tầng

    // Route tiếng Việt thân thiện (RouteMap là nguồn duy nhất). AddPageRoute THÊM route mới, route
    // mặc định PascalCase vẫn còn nhưng bị LegacyRouteRedirectMiddleware 301 sang route này.
    foreach (var (page, route) in TourKit.Api.Routing.RouteMap.Pages)
    {
        options.Conventions.AddPageRoute(page, route);
    }
    foreach (var (page, route) in TourKit.Api.Routing.RouteMap.ExtraRoutes)
    {
        options.Conventions.AddPageRoute(page, route);
    }
});

// DEV: biên dịch Razor lúc chạy → sửa .cshtml chỉ cần F5, KHÔNG phải build + khởi động lại
// (trước đây mỗi lần chỉnh giao diện mất ~40-60 giây cho vòng build/restart). Prod không bật.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
}

// Mọi section cấu hình -> lớp có kiểu, tiêm được ở bất kỳ đâu. Xem Configuration/OptionsStartup.cs.
builder.AddTourKitOptions();

// CORS cho SPA (Vite dev mặc định 5173/4173; prod cấu hình qua Cors:Origins).
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(builder.Read<TourKit.Api.Configuration.CorsOptions>(
        TourKit.Api.Configuration.CorsOptions.SectionName).ResolveOrigins())
    .AllowAnyHeader()
    .AllowAnyMethod()));

// --- Tenancy: 1 instance scoped, vừa là ITenantContext (đọc) vừa set được (login/middleware) ---
builder.Services.AddScoped<AmbientTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

// --- Cache (thư viện TourKit.Caching): có cấu hình Redis thì dùng Redis, không thì bộ nhớ tiến trình ---
builder.Services.AddTourKitCaching(builder.Read<RedisOptions>(RedisOptions.SectionName));
builder.Services.AddScoped<TourKit.Api.Services.UserDirectory>();

// --- DB provider theo cấu hình ---
var database = builder.Read<DatabaseOptions>(DatabaseOptions.SectionName);
var connectionString = builder.Configuration.GetConnectionString(DatabaseOptions.ConnectionName);
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    if (database.IsSqlServer)
    {
        opt.UseSqlServer(connectionString);
    }
    else if (database.IsPostgres)
    {
        opt.UseNpgsql(connectionString);
    }
    else
    {
        opt.UseSqlite(connectionString);
    }

    // Audit log tự động ở tầng ghi (ghi ActivityLog mỗi SaveChanges) — conventions §8.
    opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});

// --- Auth services --- (JwtOptions đăng ký ở AddTourKitOptions)
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICookieAuthService, CookieAuthService>();   // xác thực cookie cho UI Razor Pages
builder.Services.AddScoped<IExternalAuthService, TourKit.Infrastructure.Auth.ExternalAuthService>();
builder.Services.AddScoped<IUserIdentityStore, UserIdentityStore>();
// Lưu lịch sử kết quả AI theo từng bản ghi (chấm điểm/tóm tắt/soạn tin).
builder.Services.AddScoped<TourKit.Application.Ai.IAiInsightStore, TourKit.Infrastructure.Ai.AiInsightStore>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>(); // quên/đặt lại mật khẩu (token DataProtection có hạn)
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
// ICurrentUserContext (Shared) trỏ về cùng instance CurrentUser — cho Infrastructure/interceptor đọc UserId.
builder.Services.AddScoped<TourKit.Shared.Security.ICurrentUserContext>(
    sp => (TourKit.Shared.Security.ICurrentUserContext)sp.GetRequiredService<ICurrentUser>());

// --- Email (conventions §8): dev ghi log; prod dùng SMTP khi Email:Provider=Smtp (giống IFileStorage) ---
if (builder.Read<EmailOptions>(EmailOptions.SectionName).IsSmtp)
{
    builder.Services.AddScoped<TourKit.Application.Notifications.IEmailSender, TourKit.Infrastructure.Notifications.SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<TourKit.Application.Notifications.IEmailSender, TourKit.Infrastructure.Notifications.LogEmailSender>();
}

// --- SMS: dev ghi log; prod thêm provider thật (Twilio/eSMS…) + đổi Sms:Provider (giống Email) ---
builder.Services.AddScoped<TourKit.Application.Notifications.ISmsSender, TourKit.Infrastructure.Notifications.LogSmsSender>();

// --- Zalo OA: dev ghi log; prod thêm provider Zalo OA thật + đổi Zalo:Provider (giống SMS) ---
builder.Services.AddScoped<TourKit.Application.Notifications.IZaloSender, TourKit.Infrastructure.Notifications.LogZaloSender>();

// --- AI: cấu hình theo TÍNH NĂNG (Ai:Features), mỗi tính năng tự chọn nhà cung cấp + model.
// Soát ngay lúc khởi động: gõ sai tên nhà cung cấp hay quên đặt khoá đều KHÔNG gây lỗi khi chạy,
// chúng chỉ làm tính năng im lặng không hoạt động.
builder.AddTourKitAi();

// --- FluentValidation: quét validator ở tầng Application ---
builder.Services.AddValidatorsFromAssemblyContaining<TourKit.Application.Customers.Validators.CreateCustomerValidator>();

// --- Kiến trúc phân tầng: Controller → Service → IRepository<T> → EF ---
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// Repo riêng cho query phức tạp/nhiều bảng (báo cáo GROUP BY) — không dịch được bằng IRepository<T> generic.
builder.Services.AddScoped<IReportQueries, ReportQueries>();
builder.Services.AddScoped<TourKit.Application.Booking.IOrderQueries, TourKit.Infrastructure.Booking.OrderQueries>();
builder.Services.AddScoped<TourKit.Application.Rooms.IRoomAllotmentQueries, TourKit.Infrastructure.Rooms.RoomAllotmentQueries>();
builder.Services.AddScoped<TourKit.Application.Customers.ICustomerQueries, TourKit.Infrastructure.Customers.CustomerQueries>();
// Ghi bảng nối RBAC bằng hard-delete (thay tập quyền/vai trò) — repo generic chỉ soft-delete.
builder.Services.AddScoped<TourKit.Application.Admin.IRbacStore, TourKit.Infrastructure.Admin.RbacStore>();
// File storage: local ở dev (conventions §8) — đổi provider (S3/Azure) bằng cấu hình, không sửa code gọi.
builder.Services.AddScoped<TourKit.Application.Files.IFileStorage>(sp =>
    new TourKit.Infrastructure.Storage.LocalFileStorage(
        sp.GetRequiredService<TourKit.Shared.Tenancy.ITenantContext>(),
        sp.GetRequiredService<IOptions<FileStorageOptions>>().Value.ResolveLocalRoot()));
// Auto-register mọi Application service (I<X>Service → <X>Service) — khỏi khai báo tay từng cái.
builder.Services.Scan(scan => scan.FromAssemblyOf<TourKit.Application.Customers.ICustomerService>()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal)))
        .AsImplementedInterfaces().WithScopedLifetime());

// --- Background jobs (Hangfire, conventions §8) — nền hạ tầng: storage in-memory (dev), server + job.
// Tắt server dưới testhost để không nhiễu integration test (WebApplicationFactory). Job nghiệp vụ thật thêm sau.
builder.Services.AddHangfire(cfg => cfg.UseInMemoryStorage());
builder.Services.AddScoped<TourKit.Api.BackgroundJobs.HeartbeatJob>();
builder.Services.AddScoped<TourKit.Api.BackgroundJobs.CareReminderJob>();
builder.Services.AddScoped<TourKit.Api.BackgroundJobs.HoldReminderJob>();
builder.Services.AddScoped<TourKit.Api.BackgroundJobs.HoldReleaseJob>();
var enableBackgroundJobs =
    System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name?.Contains("testhost", StringComparison.OrdinalIgnoreCase) != true
    && builder.Read<BackgroundJobsOptions>(BackgroundJobsOptions.SectionName).Enabled;
if (enableBackgroundJobs)
{
    builder.Services.AddHangfireServer();
}

var jwt = builder.Read<JwtOptions>(JwtOptions.SectionName);

// CHỐT CHẶN KHỞI ĐỘNG — khoá ký JWT là thứ duy nhất giữ ranh giới giữa các tenant: ai biết khoá thì
// tự ký được token với tenant_id bất kỳ + toàn bộ quyền, đọc/ghi dữ liệu của MỌI đơn vị. Khoá mẫu
// trong appsettings.json nằm sẵn trong git, nên chạy production bằng nó là mất trắng cách ly.
// Thà hỏng lúc deploy còn hơn âm thầm chạy với khoá ai cũng biết.
if (!builder.Environment.IsDevelopment())
{
    var weak = string.IsNullOrWhiteSpace(jwt.Secret)
        || jwt.Secret.Length < 32
        || jwt.Secret.Contains("change-me", StringComparison.OrdinalIgnoreCase);
    if (weak)
    {
        throw new InvalidOperationException(
            "Jwt:Secret đang là khoá mẫu/quá ngắn. Đặt biến môi trường Jwt__Secret bằng chuỗi ngẫu nhiên >= 32 ký tự " +
            "rồi khởi động lại. KHÔNG để khoá thật trong appsettings.json (file này nằm trong git).");
    }

    // Chuỗi ENC: chỉ là che mắt (khoá Crypton hardcode trong mã nguồn) — không được dùng cho bí mật
    // thật ở môi trường chạy thật. Đặt giá trị thật qua biến môi trường; Crypton.Unwrap trả nguyên văn.
    foreach (var key in new[] { "Redis:ConnectionString", "Email:User", "Email:Password", "ConnectionStrings:Default" })
    {
        if (builder.Configuration[key]?.StartsWith("ENC:", StringComparison.Ordinal) == true)
        {
            throw new InvalidOperationException(
                $"{key} vẫn ở dạng ENC: — khoá giải nằm trong mã nguồn nên đây KHÔNG phải mã hoá. " +
                $"Đặt giá trị thật qua biến môi trường {key.Replace(":", "__", StringComparison.Ordinal)}.");
        }
    }
}

var google = builder.Read<TourKit.Api.Configuration.GoogleAuthOptions>(
    TourKit.Api.Configuration.GoogleAuthOptions.SectionName);

var authBuilder = builder.Services.AddAuthentication(options =>
    {
        // "smart": chọn scheme theo request — API gửi Bearer → JWT; trang HTML (không Bearer) → Cookie.
        options.DefaultScheme = "smart";
        options.DefaultChallengeScheme = "smart";
    })
    .AddPolicyScheme("smart", "smart", options =>
    {
        options.ForwardDefaultSelector = ctx =>
        {
            // API (/api/*) hoặc có header Bearer → JWT (challenge = 401, không redirect).
            // Còn lại (trang HTML) → Cookie (challenge = redirect /Auth/Login).
            string? auth = ctx.Request.Headers.Authorization;
            var isApi = ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
            return isApi || auth?.StartsWith("Bearer ", StringComparison.Ordinal) == true
                ? JwtBearerDefaults.AuthenticationScheme
                : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
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
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/dang-nhap";
        options.LogoutPath = "/dang-xuat";
        options.AccessDeniedPath = "/dang-nhap";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "tourkit_auth";
        // Mặc định của SecurePolicy là SameAsRequest → chỉ cần MỘT lần điều hướng qua HTTP là cookie
        // phiên đi ra ngoài dạng rõ và ai đứng giữa mạng cũng nhặt được (quán cà phê, ISP, chặng đầu
        // trước reverse proxy). Ép Always. SameSite=Lax chặn cookie bị gửi kèm request từ trang khác.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest   // dev chạy http://localhost, ép Always sẽ mất phiên
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    // Cookie tạm của chặng đăng nhập ngoài. Đăng ký VÔ ĐIỀU KIỆN, kể cả khi chưa có khoá Google:
    // các trang và test đọc scheme này qua tên, thiếu nó thì lỗi là "scheme không tồn tại" ở giữa
    // luồng chứ không phải một thông báo nói rõ chưa bật Google.
    .AddCookie(TourKit.Api.Auth.ExternalAuthDefaults.Scheme, options =>
    {
        options.ExpireTimeSpan = TourKit.Api.Auth.ExternalAuthDefaults.Lifetime;
        options.SlidingExpiration = false;   // 10 phút là 10 phút, không gia hạn theo thao tác
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = TourKit.Api.Auth.ExternalAuthDefaults.Scheme;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

if (google.IsConfigured)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = google.ClientId;
        options.ClientSecret = google.ClientSecret;

        // Kết quả của Google đi vào cookie TẠM, không vào cookie đăng nhập chính: lúc này mới biết
        // "email này đã được Google xác minh", chưa biết nó thuộc tài khoản nào bên mình.
        options.SignInScheme = TourKit.Api.Auth.ExternalAuthDefaults.Scheme;

        // Không có claim này thì không cách nào phân biệt email đã xác minh với email người ta tự
        // khai — mà nhận nhầm email chưa xác minh là trao thẳng tài khoản cho người khai bừa.
        options.ClaimActions.MapJsonKey("email_verified", "email_verified", "boolean");
    });
}

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

    // Chỉ Development: seed bộ data mẫu vào DB để kiểm tra trực quan các thanh lọc.
    if (app.Environment.IsDevelopment() && db.Database.IsRelational())
    {
        var ambient = scope.ServiceProvider.GetRequiredService<TourKit.Infrastructure.Tenancy.AmbientTenantContext>();
        await TourKit.Api.DevData.DemoDataSeeder.SeedAsync(
            db,
            ambient,
            scope.ServiceProvider.GetRequiredService<TourKit.Application.Provisioning.IProvisioningService>(),
            scope.ServiceProvider.GetRequiredService<TourKit.Application.Auth.IPasswordHasher>());

        // DEV-ONLY: bơm dữ liệu NEO VÀO HÔM NAY để các màn thống kê có gì mà hiện.
        // Hai seeder trên rải dữ liệu quanh lúc CHÚNG chạy; xem lại sau vài tuần thì mọi mốc đã
        // trôi vào quá khứ và Bàn làm việc trông như hỏng dù dữ liệu vẫn đúng.
        await TourKit.Api.DevData.RecentDataSeeder.SeedAsync(db, ambient);

        // DEV-ONLY: bơm KHỐI LƯỢNG LỚN (hàng nghìn dòng/bảng) để test hiệu năng lưới/phân trang.
        // Chỉ chạy khi đặt biến môi trường SEED_PERF_COUNT>0 (vd SEED_PERF_COUNT=3000). Idempotent.
        var perfCountStr = Environment.GetEnvironmentVariable("SEED_PERF_COUNT");
        if (app.Environment.IsDevelopment() && int.TryParse(perfCountStr, out var perfCount) && perfCount > 0)
        {
            await TourKit.Api.DevData.PerfDataSeeder.SeedAsync(db, ambient, perfCount);
        }
    }
}

app.UseSerilogRequestLogging();   // log mỗi request (method/path/status/thời gian) có cấu trúc
app.UseMiddleware<ExceptionHandlingMiddleware>();   // thay UseExceptionHandler: map AppException → HTTP + ProblemDetails
app.UseStatusCodePages();

// Header an toàn cho MỌI phản hồi (đặt sớm để áp cả trang lỗi lẫn file tĩnh).
// - nosniff: chặn trình duyệt tự đoán kiểu nội dung (đi kèm với việc file tải lên giữ nguyên content-type).
// - X-Frame-Options DENY: chặn nhúng iframe → chặn clickjacking, tức chặn việc lừa quản trị bấm nhầm
//   vào thao tác trên màn Người dùng/Vai trò.
// - Referrer-Policy: không rò đường dẫn nội bộ (có id bản ghi) sang site ngoài.
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Số liệu hiển thị theo kiểu Việt: 45.742.253.000 (không phải 45,742,253,000).
// Trước đây phía server render bằng văn hoá mặc định (dấu phẩy) còn lưới render bằng vi-VN (dấu chấm)
// → cùng một màn có hai kiểu số. Đặt vi-VN cho toàn bộ request là hết lệch.
// KHÔNG ảnh hưởng nhập liệu: value provider của ASP.NET Core (form/query/route) luôn parse bằng
// InvariantCulture, nên số thập phân gửi lên vẫn hiểu đúng.
var vi = new System.Globalization.CultureInfo("vi-VN");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(vi),
    SupportedCultures = [vi],
    SupportedUICultures = [vi],
});

app.UseCors("web");   // trước Authentication để preflight OPTIONS không cần token

app.UseStaticFiles();   // phục vụ wwwroot (assets Vuexy) — đặt trước redirect để asset không bị 301

// 301 URL cũ (PascalCase) → route tiếng Việt. Sau static files (asset không dính), trước auth/routing.
app.UseMiddleware<TourKit.Api.Routing.LegacyRouteRedirectMiddleware>();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // sau Authentication để đọc được claim
app.UseMiddleware<SubscriptionGuardMiddleware>();  // chặn nếu subscription hết hạn (miễn trừ auth/đăng ký/billing)
app.UseAuthorization();

if (enableBackgroundJobs)
{
    // Dashboard chỉ cho user đã xác thực; job heartbeat mỗi 30 phút (demo hạ tầng).
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new TourKit.Api.BackgroundJobs.HangfireDashboardAuthFilter()],
    });
    RecurringJob.AddOrUpdate<TourKit.Api.BackgroundJobs.HeartbeatJob>("heartbeat", j => j.Run(), "*/30 * * * *");
    // Gửi chăm sóc tự động (Đợt 7): quét lịch CSKH tới hạn nhắc mỗi giờ, email người phụ trách.
    RecurringJob.AddOrUpdate<TourKit.Api.BackgroundJobs.CareReminderJob>(
        "care-reminders", j => j.RunAsync(CancellationToken.None), "0 * * * *");
    // Nhắc hạn giữ chỗ (Đợt 7): chỗ giữ sắp hết hạn (≤24h) → email sales phụ trách; lệch 15' tránh trùng giờ.
    RecurringJob.AddOrUpdate<TourKit.Api.BackgroundJobs.HoldReminderJob>(
        "hold-reminders", j => j.RunAsync(CancellationToken.None), "15 * * * *");
    // Tự động nhả chỗ giữ hết hạn (P0-1): quét mỗi 10' các chỗ giữ quá hạn HoldExpiresAt → huỷ để giải phóng slot.
    RecurringJob.AddOrUpdate<TourKit.Api.BackgroundJobs.HoldReleaseJob>(
        "hold-releases", j => j.RunAsync(CancellationToken.None), "*/10 * * * *");
}

app.MapControllers();   // Customers, Providers, Crm (kiến trúc phân tầng)
app.MapRazorPages();    // UI Razor Pages (Vuexy) — cùng process với REST


app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
