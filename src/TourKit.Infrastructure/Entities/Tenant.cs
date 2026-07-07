using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
