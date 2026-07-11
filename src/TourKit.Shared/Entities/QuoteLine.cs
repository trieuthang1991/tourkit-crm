namespace TourKit.Shared.Entities;

/// <summary>Một dòng của <see cref="Quote"/>: mô tả hạng mục, số lượng × đơn giá. Amount = Quantity × UnitPrice (tính khi đọc).</summary>
public sealed class QuoteLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid QuoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
