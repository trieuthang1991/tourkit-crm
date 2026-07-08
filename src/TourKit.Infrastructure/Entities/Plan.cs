using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Gói dịch vụ SaaS (global — KHÔNG thuộc tenant). Seed từ catalog code.</summary>
public sealed class Plan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxTours { get; set; }
    public decimal PriceMonthly { get; set; }
}
