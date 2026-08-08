using System.Security.Claims;
using TourKit.Application.Auth;

namespace TourKit.Api.Auth;

/// <summary>
/// Chuyển principal Google trả về thành <see cref="ExternalIdentity"/>, hoặc từ chối.
///
/// Tách riêng khỏi PageModel vì đây là chốt chặn an ninh, không phải mã điều hướng: nó là nơi duy
/// nhất quyết định "bộ claim này có đủ tin để đem đi tra tài khoản không". Ở dạng hàm thuần thì
/// kiểm thử được từng trường hợp thiếu claim mà không phải dựng cả một request HTTP.
/// </summary>
public static class GoogleIdentityReader
{
    /// <summary>Tên nhà cung cấp ghi xuống CSDL. Đặt một chỗ để không có nơi ghi "google" nơi ghi "Google".</summary>
    public const string Provider = "Google";

    public static bool TryRead(ClaimsPrincipal? principal, out ExternalIdentity? identity)
    {
        identity = null;
        if (principal is null)
        {
            return false;
        }

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);

        // Thiếu một trong hai thì không có gì để tra: subject là thứ duy nhất Google giữ nguyên khi
        // người dùng đổi email, còn email là thứ duy nhất khớp được với tài khoản đã có sẵn.
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // Google cho phép tài khoản có email CHƯA xác minh (tài khoản doanh nghiệp tự tạo, hoặc email
        // vừa đổi chưa bấm xác nhận). Nhận email chưa xác minh nghĩa là ai khai được email của người
        // khác thì chiếm được tài khoản của người đó — nên thiếu bằng chứng xác minh là từ chối.
        var verified = principal.FindFirstValue("email_verified");
        if (!string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = principal.FindFirstValue(ClaimTypes.Name);
        identity = new ExternalIdentity(
            Provider,
            subject.Trim(),
            email.Trim(),
            EmailVerified: true,
            DisplayName: string.IsNullOrWhiteSpace(name) ? null : name.Trim());
        return true;
    }
}
