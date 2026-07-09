
namespace TourKit.Shared.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
