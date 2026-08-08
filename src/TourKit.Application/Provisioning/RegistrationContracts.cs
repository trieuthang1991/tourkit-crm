namespace TourKit.Application.Provisioning;

public sealed record RegisterTenantRequest(
    string CompanyName, string Slug, string AdminEmail, string AdminPassword, string AdminFullName);

/// <summary>
/// Đăng ký công ty qua nhà cung cấp ngoài. KHÔNG có mật khẩu: email đã được nhà cung cấp xác minh,
/// còn mật khẩu do hệ thống tự sinh ngẫu nhiên rồi băm và vứt bỏ ngay — không ai, kể cả người dùng,
/// biết chuỗi đó là gì.
/// </summary>
public sealed record RegisterExternalTenantRequest(
    string CompanyName, string Slug, string AdminEmail, string AdminFullName,
    string Provider, string ProviderSubject);

public sealed record RegistrationResponse(Guid TenantId, string Slug, Guid AdminUserId);
