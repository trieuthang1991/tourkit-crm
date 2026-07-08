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

    public const string LeadView = "lead.view";
    public const string LeadCreate = "lead.create";
    public const string LeadUpdate = "lead.update";
    public const string LeadDelete = "lead.delete";
    public const string LeadConvert = "lead.convert";

    public const string DepartureView = "departure.view";
    public const string DepartureCreate = "departure.create";
    public const string BookingView = "booking.view";
    public const string BookingCreate = "booking.create";
    public const string BookingSeatConfirm = "booking.seat.confirm";   // hệ cũ: TR_TM_XNC (xác nhận chỗ)

    public const string ReceiptView = "receipt.view";
    public const string ReceiptCreate = "receipt.create";

    /// <summary>Toàn bộ mã quyền + nhóm hiển thị. Dùng để seed + đăng ký policy.</summary>
    public static readonly IReadOnlyList<(string Code, string Group)> All =
    [
        (CustomerView, "Customer"), (CustomerCreate, "Customer"),
        (CustomerUpdate, "Customer"), (CustomerDelete, "Customer"),
        (TourView, "Catalog"), (TourCreate, "Catalog"),
        (TourUpdate, "Catalog"), (TourDelete, "Catalog"),
        (LeadView, "CRM"), (LeadCreate, "CRM"), (LeadUpdate, "CRM"),
        (LeadDelete, "CRM"), (LeadConvert, "CRM"),
        (DepartureView, "Booking"), (DepartureCreate, "Booking"),
        (BookingView, "Booking"), (BookingCreate, "Booking"),
        (BookingSeatConfirm, "Booking"),
        (ReceiptView, "Finance"), (ReceiptCreate, "Finance"),
    ];
}
