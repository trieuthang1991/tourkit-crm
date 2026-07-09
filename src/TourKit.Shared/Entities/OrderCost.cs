
namespace TourKit.Shared.Entities;

/// <summary>
/// Chi phí trả nhà cung cấp cho 1 dòng trong đơn — grounded ở legacy bảng `Order_Chi`.
/// Legacy còn check_email/date_signed/ServiceCodeInTour — deferred. ServiceId FK legacy được thay
/// bằng ServiceName (string) cho MVP vì catalog dịch vụ chưa tồn tại trong slice này.
/// </summary>
public sealed class OrderCost : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProviderId { get; set; }
    public string? ServiceName { get; set; }
    public int DayIndex { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Deposit { get; set; }
    public decimal Surcharge { get; set; }
    public decimal Vat { get; set; }
    public int Status { get; set; }
}
