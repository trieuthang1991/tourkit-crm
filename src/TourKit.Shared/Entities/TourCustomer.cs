
namespace TourKit.Shared.Entities;

/// <summary>
/// Dòng đặt chỗ theo từng khách trên 1 đơn (legacy tour_customers) — mỗi Order có thể có nhiều dòng.
/// Legacy còn các cột visa/vân tay (date_fingerprinting, signature_xndv...), branch/agency
/// (IdBranch, idTAAgency, codeTAAgency...) — deferred, chưa cần cho slice này.
/// </summary>
public sealed class TourCustomer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid TourDepartureId { get; set; }
    public Guid CustomerId { get; set; }

    // Số lượng theo độ tuổi (legacy quantity/amount_children/amount_children_small/quantity_baby).
    public int Quantity { get; set; }
    public int AmountChildren { get; set; }
    public int AmountChildrenSmall { get; set; }
    public int QuantityBaby { get; set; }

    // Đơn giá theo độ tuổi.
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public decimal PriceChildSmall { get; set; }
    public decimal PriceBaby { get; set; }

    // Phụ thu theo độ tuổi.
    public decimal Surcharge { get; set; }
    public decimal ChildSurcharge { get; set; }
    public decimal ChildSurchargeSmall { get; set; }
    public decimal BabySurcharge { get; set; }

    // Giảm giá theo độ tuổi.
    public decimal Discount { get; set; }
    public decimal ChildDiscount { get; set; }
    public decimal ChildDiscountSmall { get; set; }
    public decimal BabyDiscount { get; set; }

    // Hoa hồng theo độ tuổi.
    public decimal Commission { get; set; }
    public decimal ChildCommission { get; set; }
    public decimal ChildCommissionSmall { get; set; }
    public decimal BabyCommission { get; set; }

    public decimal UpfrontAmount { get; set; }
    public string? ReservationCode { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    /// <summary>Job nhắc-hạn-giữ-chỗ đã email sales lúc nào (idempotency — mỗi chỗ giữ nhắc 1 lần). Null = chưa nhắc.</summary>
    public DateTimeOffset? HoldReminderSentAt { get; set; }
    public string? SeatSelected { get; set; }
    public bool IsMainContact { get; set; }
    public int Status { get; set; }
}
