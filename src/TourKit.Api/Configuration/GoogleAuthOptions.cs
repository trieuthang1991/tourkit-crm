namespace TourKit.Api.Configuration;

/// <summary>
/// Cấu hình đăng nhập Google. Khoá thật KHÔNG nằm trong file theo git — đặt qua biến môi trường
/// <c>Authentication__Google__ClientId</c> / <c>Authentication__Google__ClientSecret</c>.
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>Chủ ý bật tính năng. Bật mà thiếu khoá thì vẫn coi như chưa cấu hình.</summary>
    public bool Enabled { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Chỉ khi cả ba đều có thì mới đăng ký scheme Google và mới hiện nút trên trang đăng nhập.
    ///
    /// Đây là lý do ứng dụng vẫn chạy được trên máy chưa xin khoá Google: thiếu khoá thì nút biến
    /// mất và đăng nhập bằng mật khẩu vẫn nguyên vẹn, thay vì chết ngay lúc khởi động vì
    /// <c>AddGoogle</c> ném ra "ClientId must be provided".
    /// </summary>
    public bool IsConfigured => Enabled
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
