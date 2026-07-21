using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Application.Notifications;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Auth;

/// <summary>
/// Quên/đặt lại mật khẩu KHÔNG cần bảng token: token là chuỗi DataProtection CÓ HẠN chứa
/// userId|tenantId|stamp, trong đó stamp băm từ PasswordHash hiện tại → đổi mật khẩu xong thì
/// mọi token cũ tự vô hiệu (dùng một lần trên thực tế).
/// </summary>
public interface IPasswordResetService
{
    /// <summary>Gửi link đặt lại. LUÔN trả về như nhau để không lộ email/tenant nào tồn tại.</summary>
    Task SendResetLinkAsync(string tenantSlug, string email, Func<string, string> buildUrl, CancellationToken ct = default);

    /// <summary>Đặt mật khẩu mới từ token. Trả về null nếu OK, ngược lại là thông báo lỗi.</summary>
    Task<string?> ResetAsync(string token, string newPassword, CancellationToken ct = default);
}

public sealed class PasswordResetService : IPasswordResetService
{
    private const string Purpose = "TourKit.PasswordReset.v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _email;
    private readonly AmbientTenantContext _tenant;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly ITimeLimitedDataProtector _protector;

    public PasswordResetService(AppDbContext db, IPasswordHasher hasher, IEmailSender email,
        AmbientTenantContext tenant, ILogger<PasswordResetService> logger, IDataProtectionProvider dp)
    {
        _db = db;
        _hasher = hasher;
        _email = email;
        _tenant = tenant;
        _logger = logger;
        _protector = dp.CreateProtector(Purpose).ToTimeLimitedDataProtector();
    }

    private static string Stamp(string passwordHash)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];

    public async Task SendResetLinkAsync(string tenantSlug, string email, Func<string, string> buildUrl, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted, ct);
        if (tenant is null)
        {
            return;
        }

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email && !u.IsDeleted, ct);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var payload = string.Create(CultureInfo.InvariantCulture, $"{user.Id}|{user.TenantId}|{Stamp(user.PasswordHash)}");
        var token = _protector.Protect(payload, Lifetime);
        var url = buildUrl(token);

        var body = $"""
            <p>Xin chào {user.FullName},</p>
            <p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu TourKit cho doanh nghiệp <b>{tenant.Slug}</b>.</p>
            <p><a href="{url}">Bấm vào đây để đặt mật khẩu mới</a></p>
            <p>Liên kết có hiệu lực trong 2 giờ. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            """;
        // SMTP hỏng (SES từ chối/timeout) KHÔNG được làm trang 500: nuốt lỗi + ghi log để quản trị soi,
        // người dùng vẫn thấy đúng một thông điệp trung lập như mọi trường hợp khác.
        try
        {
            await _email.SendAsync(user.Email, "TourKit — Đặt lại mật khẩu", body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email đặt lại mật khẩu thất bại cho {Email}", user.Email);
        }
    }

    public async Task<string?> ResetAsync(string token, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "Liên kết không hợp lệ.";
        }

        string payload;
        try
        {
            payload = _protector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            return "Liên kết đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.";
        }

        var parts = payload.Split('|');
        if (parts.Length != 3 || !Guid.TryParse(parts[0], out var userId))
        {
            return "Liên kết không hợp lệ.";
        }

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
        if (user is null || !user.IsActive)
        {
            return "Tài khoản không tồn tại hoặc đã bị khoá.";
        }

        // Stamp lệch = mật khẩu đã đổi sau khi phát token → token cũ hết giá trị.
        if (!string.Equals(parts[2], Stamp(user.PasswordHash), StringComparison.Ordinal))
        {
            return "Liên kết đã được sử dụng. Vui lòng yêu cầu lại.";
        }

        // Request ẩn danh chưa có tenant context → set theo user vừa tra, nếu không guard chéo tenant sẽ chặn lưu.
        _tenant.SetTenant(user.TenantId);
        user.PasswordHash = _hasher.Hash(newPassword);

        // THU HỒI refresh token đang sống. Người ta đặt lại mật khẩu chính vì nghi bị chiếm tài khoản;
        // nếu không thu hồi thì refresh token của kẻ chiếm vẫn dùng được thêm nhiều ngày, tức thao tác
        // "khôi phục" không hề khôi phục được gì.
        var now = DateTimeOffset.UtcNow;
        var live = await _db.RefreshTokens.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var rt in live)
        {
            rt.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Đặt lại mật khẩu thành công cho user {UserId}, thu hồi {Count} refresh token.", user.Id, live.Count);
        }

        return null;
    }
}
