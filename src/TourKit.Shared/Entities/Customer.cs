
namespace TourKit.Shared.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
