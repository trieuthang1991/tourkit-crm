namespace TourKit.Shared.Entities;

/// <summary>
/// Quỹ vé ứng (legacy TicketFund): vé/dịch vụ NCC cấp ứng cho một đơn, theo dõi mã vé + đóng quỹ.
/// Bám legacy: OrderId, ProviderId, ProviderServicePricingId→ProviderServiceId, TicketCode, Status, IsClose→IsClosed.
/// </summary>
public sealed class TicketFund : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }               // đơn được cấp vé (legacy OrderId)
    public Guid? ProviderId { get; set; }           // NCC cấp vé (legacy ProviderId)
    public Guid? ProviderServiceId { get; set; }    // giá dịch vụ (legacy ProviderServicePricingId)
    public string TicketCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool IsClosed { get; set; }              // legacy IsClose
}
