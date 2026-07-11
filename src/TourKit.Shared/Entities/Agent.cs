namespace TourKit.Shared.Entities;

/// <summary>
/// Đại lý B2B (B2B Agent Portal §4.2.1) — doanh nghiệp đối tác đặt dịch vụ qua Portal.
/// Tài khoản do DMC cấp (đại lý không tự đăng ký ở MVP). Có hạn mức tín dụng để kiểm soát công nợ (§4.2.7).
/// </summary>
public sealed class Agent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }   // hạn mức công nợ
    public int Status { get; set; }             // 0 ngừng, 1 hoạt động
}
