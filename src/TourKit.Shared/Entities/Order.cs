
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

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
    public Guid? SalesUserId { get; set; }
    public Guid? CreatedByUserId { get; set; } // Người tạo đơn (legacy id_sales_root_create)
    public Guid? BranchId { get; set; }        // Chi nhánh (legacy ChiNhanh) — lọc/báo cáo theo chi nhánh
    public Guid? MarketTypeId { get; set; }    // Thị trường (legacy MarketType) — lọc nhanh theo thị trường
    public Guid? TourGroupId { get; set; }     // Nhóm tour (legacy Nhóm) — gom tour theo nhóm
    public int BookingType { get; set; }       // Loại tour: 0 FIT,1 GIT,2 LandTour/Combo,3 Booking phòng,4 Dịch vụ lẻ,5 Visa,6 Xe
    public bool IsCommissionSettled { get; set; } // TT hoa hồng: đã chốt hoa hồng chưa (legacy)
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    // Tổng tiền denormalized (legacy Total_Thu_Money / Total_Chi_Money / TotalRefund / ApprovedRevenue).
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalRefund { get; set; }
    public decimal ApprovedRevenue { get; set; }

    public bool IsPaymentRecognized { get; set; }
}
