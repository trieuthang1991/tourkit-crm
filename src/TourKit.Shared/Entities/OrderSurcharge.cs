namespace TourKit.Shared.Entities;

/// <summary>
/// Dòng phụ thu của 1 đơn (legacy SurchargeServices): khoản KH trả thêm (phòng đơn, cao điểm…).
/// Thành tiền (<see cref="Amount"/>) cộng thẳng vào <see cref="Order.TotalRevenue"/> nên tự chảy vào
/// công nợ/hoa hồng/báo cáo. Bất biến: TotalRevenue = giá gốc + Σ Amount → % luôn tính trên giá gốc.
/// </summary>
public sealed class OrderSurcharge : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? SurchargeId { get; set; }             // tham chiếu danh mục (tuỳ chọn)
    public string Description { get; set; } = string.Empty;
    public int CalcType { get; set; }                  // SurchargeCalcType (0 Fixed, 1 Percent)
    public decimal Value { get; set; }                 // số tiền (Fixed) hoặc % (Percent)
    public decimal Amount { get; set; }                // thành tiền đã tính (lưu để cộng/trừ chính xác)
}
