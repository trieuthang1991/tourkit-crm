namespace TourKit.Shared.Entities;

/// <summary>
/// Tỷ giá tiền tệ (legacy <c>ExchangeRate</c>): quy đổi giá vốn NCC ngoại tệ về VND.
/// <see cref="RateToVnd"/> = 1 đơn vị tiền này bằng bao nhiêu VND (VND: Code="VND", Rate=1).
/// Thuần catalog, <see cref="Code"/> duy nhất theo tenant.
/// </summary>
public sealed class Currency : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;   // USD, EUR, VND…
    public string Name { get; set; } = string.Empty;
    public decimal RateToVnd { get; set; }             // 1 <Code> = RateToVnd VND
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
