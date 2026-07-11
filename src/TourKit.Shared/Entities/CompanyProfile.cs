namespace TourKit.Shared.Entities;

/// <summary>
/// Hồ sơ công ty (legacy <c>Config</c> — phần thông tin doanh nghiệp, KHÔNG gồm credential/API key ngoài):
/// tên/địa chỉ/MST/người đại diện/giấy phép/tài khoản ngân hàng. Mỗi tenant 1 bản (singleton). Dùng làm
/// BÊN A trên hợp đồng in + tiêu đề báo giá thay cho placeholder cứng.
/// </summary>
public sealed class CompanyProfile : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;      // NameCompany
    public string? ShortName { get; set; }                // shortnameCompany
    public string? Address { get; set; }
    public string? Hotline { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxCode { get; set; }                  // masothue
    public string? LegalRepName { get; set; }             // nguoidaidien
    public string? LegalRepTitle { get; set; }            // chucvu
    public string? LicenseNumber { get; set; }            // sogiayphep
    public string? BankAccount { get; set; }              // AccountBankCompany
}
