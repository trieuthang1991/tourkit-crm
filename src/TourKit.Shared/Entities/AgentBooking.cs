namespace TourKit.Shared.Entities;

/// <summary>
/// Đặt chỗ của Đại lý (B2B Portal §4.2.4) — tạo từ một <see cref="AgentQuoteRequest"/> đã Confirmed.
/// TotalAmount lấy từ giá đã chào (QuotedAmount). Gồm nhiều <see cref="AgentPassenger"/>.
/// </summary>
public sealed class AgentBooking : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AgentId { get; set; }
    public Guid QuoteRequestId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }   // 0 chờ, 1 xác nhận, 2 huỷ, 3 hoàn tất
    public string? Note { get; set; }
}
