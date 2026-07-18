namespace TourKit.Shared.Entities;

/// <summary>
/// Lịch thanh toán NCC theo dịch vụ (legacy ServicePaymentTerm) — một dòng = một đợt/kỳ chi trả
/// (số tiền + hạn chi) gắn vào một <see cref="ServiceBooking"/>. Nhiều dòng tạo thành lịch trả góp/đặt cọc.
/// Trạng thái đợt: 0 = chờ chi, 1 = đã chi. PaidAmount ghi số đã chi cho đợt; PaymentVoucherId (tuỳ chọn)
/// nối tới phiếu chi đã ghi nhận dòng tiền cho đợt này.
/// </summary>
public sealed class ServicePaymentTerm : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>FK bắt buộc: đợt chi thuộc lịch thanh toán của một booking dịch vụ (legacy per-service schedule).</summary>
    public Guid ServiceBookingId { get; set; }

    /// <summary>Soft-FK tuỳ chọn tới dòng chi phí NCC (<see cref="OrderCost"/>) mà đợt này gắn vào — null nếu chỉ theo booking.</summary>
    public Guid? OrderCostId { get; set; }

    public decimal Amount { get; set; }                 // số tiền đợt chi
    public DateTimeOffset DueDate { get; set; }          // hạn chi (đến hạn / quá hạn tính từ đây)
    public string? Note { get; set; }
    public int Status { get; set; }                      // 0 = chờ chi, 1 = đã chi
    public decimal PaidAmount { get; set; }              // đã chi cho đợt (mặc định 0)

    /// <summary>Soft-FK tuỳ chọn tới phiếu chi (<see cref="PaymentVoucher"/>) đã ghi nhận cho đợt này.</summary>
    public Guid? PaymentVoucherId { get; set; }
}
