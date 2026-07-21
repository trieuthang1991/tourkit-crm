namespace TourKit.Application.Auth;

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
