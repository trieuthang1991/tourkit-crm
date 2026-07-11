
namespace TourKit.Shared.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int CustomerType { get; set; }      // legacy id_customer_type — dùng cho hoa hồng theo loại khách
    public string? Source { get; set; }        // nguồn khách (legacy Source)
    public string? Tag { get; set; }           // nhãn phân loại (legacy Tag)
    public decimal TempBalance { get; set; }   // tạm ứng (legacy TempBalance)
}
