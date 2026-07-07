namespace TourKit.Api.Authz;

/// <summary>Catalog mã quyền nền tảng (global). Map từ hệ cũ — xem database-optimization-analysis §J.</summary>
public static class Permissions
{
    public const string CustomerView = "customer.view";
    public const string CustomerCreate = "customer.create";
    public const string CustomerUpdate = "customer.update";
    public const string CustomerDelete = "customer.delete";

    public const string TourView = "tour.view";
    public const string TourCreate = "tour.create";
    public const string TourUpdate = "tour.update";
    public const string TourDelete = "tour.delete";

    /// <summary>Toàn bộ mã quyền + nhóm hiển thị. Dùng để seed + đăng ký policy.</summary>
    public static readonly IReadOnlyList<(string Code, string Group)> All =
    [
        (CustomerView, "Customer"), (CustomerCreate, "Customer"),
        (CustomerUpdate, "Customer"), (CustomerDelete, "Customer"),
        (TourView, "Catalog"), (TourCreate, "Catalog"),
        (TourUpdate, "Catalog"), (TourDelete, "Catalog"),
    ];
}
