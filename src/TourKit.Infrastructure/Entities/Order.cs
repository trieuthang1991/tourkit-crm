using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>
/// Đơn hàng (header) — gom các dòng đặt chỗ (<see cref="TourCustomer"/>) của 1 khách trên 1 chuyến.
/// Tổng tiền lưu denormalized như legacy Orders (Total_Thu_Money/Total_Chi_Money/TotalRefund/ApprovedRevenue).
/// Legacy còn nhiều biến thể tổng khác (TotalRevenueRoot, UnapprovedRevenue, ApprovedSpent, UnapprovedSpent,
/// TotalSpentRoot...) — deferred, chưa cần cho slice này.
/// </summary>
public sealed class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid TourDepartureId { get; set; }
    public Guid CustomerId { get; set; }
    public int BookingType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    // Tổng tiền denormalized (legacy Total_Thu_Money / Total_Chi_Money / TotalRefund / ApprovedRevenue).
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalRefund { get; set; }
    public decimal ApprovedRevenue { get; set; }

    public bool IsPaymentRecognized { get; set; }
}
