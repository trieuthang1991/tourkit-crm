namespace TourKit.Api.Billing;

/// <summary>Catalog gói dịch vụ SaaS (global). Nguồn seed cho bảng Plans.</summary>
public static class PlanCatalog
{
    public const string DefaultPlanCode = "free";

    /// <summary>Toàn bộ gói: mã, tên, giới hạn user/tour, giá theo tháng. Dùng để seed.</summary>
    public static readonly IReadOnlyList<(string Code, string Name, int MaxUsers, int MaxTours, decimal PriceMonthly)> All =
    [
        ("free", "Free", 3, 10, 0m),
        ("pro", "Pro", 20, 500, 990_000m),
    ];
}
