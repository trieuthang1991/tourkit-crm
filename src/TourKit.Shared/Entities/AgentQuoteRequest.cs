using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Yêu cầu báo giá của Đại lý (B2B Portal §4.2.3 — module trọng tâm MVP). Đại lý gửi yêu cầu (sản phẩm,
/// ngày, số khách, yêu cầu riêng); Sales tính giá NGOÀI Portal rồi điền <see cref="QuotedAmount"/> (Portal
/// không tự tính giá ở MVP). Quote <c>Confirmed</c> là điều kiện tạo Booking.
/// </summary>
public sealed class AgentQuoteRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AgentId { get; set; }
    public string ProductName { get; set; } = string.Empty;   // sản phẩm/tour đại lý muốn hỏi giá
    public DateTimeOffset? TravelDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public int PaxCount { get; set; }
    public string? SpecialRequests { get; set; }
    public AgentQuoteStatus Status { get; set; } = AgentQuoteStatus.Requested;
    public decimal? QuotedAmount { get; set; }                // Sales điền khi chào giá
    public string? QuotedNote { get; set; }
}
