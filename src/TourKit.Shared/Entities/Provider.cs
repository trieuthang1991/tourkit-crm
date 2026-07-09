
namespace TourKit.Shared.Entities;

/// <summary>
/// Nhà cung cấp dịch vụ (khách sạn, xe, nhà hàng, HDV, hãng bay...) — grounded ở legacy bảng `providers`.
/// Legacy còn class_hotel_id/car_type/HDV fields (Skill/Languages/Gender) theo từng loại NCC — deferred,
/// chưa cần cho slice chi phí MVP.
/// </summary>
public sealed class Provider : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProviderType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public int Rate { get; set; }
    public int Status { get; set; }
}
