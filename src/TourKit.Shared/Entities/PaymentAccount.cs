namespace TourKit.Shared.Entities;

/// <summary>
/// Tài khoản/phương thức nhận tiền (legacy <c>PaymentMethod</c>): thông tin ngân hàng để IN lên
/// báo giá/hoá đơn (tên hiển thị, ngân hàng, số TK, chủ TK, nội dung CK mặc định). Tên đặt khác
/// <c>PaymentMethod</c> vì cái đó đã là property string trên Receipt/PaymentVoucher.
/// Chỉ 1 tài khoản <see cref="IsDefault"/> mỗi tenant (in mặc định).
/// </summary>
public sealed class PaymentAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // NameMethod — tên hiển thị, vd "VCB - Cty ABC"
    public string? BankName { get; set; }              // BankName
    public string? AccountNumber { get; set; }         // AccountNumber
    public string? AccountHolder { get; set; }         // chủ tài khoản
    public string? Branch { get; set; }                // chi nhánh
    public string? TransferNote { get; set; }          // nội dung CK mặc định (nội dung ghi khi khách chuyển)
    public bool IsDefault { get; set; }                // tài khoản in mặc định (duy nhất/tenant)
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
