namespace TourKit.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "tourkit";
    public string Audience { get; set; } = "tourkit";
    public string Secret { get; set; } = string.Empty;       // >= 32 ký tự; nạp từ config/secret
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
