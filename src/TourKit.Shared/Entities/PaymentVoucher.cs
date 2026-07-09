
namespace TourKit.Shared.Entities;

/// <summary>
/// Phiếu chi (legacy N_PaymentVoucher, subset) — tiền công ty chi trả cho NCC theo một đơn.
/// Đối xứng ReceiptVoucher (phiếu thu). Ghi nhận dòng tiền qua IsRecognized (legacy IsGhiNhanDongTien).
/// Legacy còn signature/fileUpload/TourGuideId/IsAutoChuyen/ParentId chuyển tiếp — deferred.
/// </summary>
public sealed class PaymentVoucher : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTimeOffset IssuedAt { get; set; }

    public Guid OrderId { get; set; }
    public Guid? ProviderId { get; set; }      // người nhận tiền = NCC (legacy Receiver + Order_Provider_Money_Id)
    public Guid? OrderCostId { get; set; }      // dòng chi phí được thanh toán (legacy Order_Provider_Money_Id)

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Partner { get; set; }
    public string? ReceiverName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Note { get; set; }

    public int Status { get; set; }             // 0 = chờ duyệt, 1 = đã duyệt, 2 = từ chối
    public bool IsClosed { get; set; }
    public bool IsRecognized { get; set; }      // legacy IsGhiNhanDongTien — chỉ true khi duyệt
    public Guid? ParentId { get; set; }
}
