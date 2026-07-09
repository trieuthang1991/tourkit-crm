
namespace TourKit.Shared.Entities;

/// <summary>
/// Phiếu thu (legacy N_ReceiptVoucher, subset) — tiền khách nộp cho một đơn.
/// Legacy còn cột ngân hàng (BankUserName/BankUserNumber/BankName), phân loại (LoaiPhieuChi),
/// duyệt (IdUserSign/UserApprove_Department/TrangThaiChi) và PreReceipt — deferred sang giai đoạn duyệt Finance.
/// </summary>
public sealed class ReceiptVoucher : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTimeOffset IssuedAt { get; set; }

    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Partner { get; set; }
    public string? ReceiverName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Note { get; set; }

    public int Status { get; set; }
    public bool IsClosed { get; set; }
    public bool IsRecognized { get; set; }
    public Guid? ParentId { get; set; }
}
