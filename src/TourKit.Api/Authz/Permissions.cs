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
    public const string DepartureClose = "departure.close";
    public const string BookingView = "booking.view";
    public const string BookingCreate = "booking.create";
    public const string BookingSeatConfirm = "booking.seat.confirm";   // hệ cũ: TR_TM_XNC (xác nhận chỗ)
    public const string BookingSeatCancel = "booking.seat.cancel";     // huỷ chỗ + hoàn tiền

    public const string ReceiptView = "receipt.view";
    public const string ReceiptCreate = "receipt.create";
    public const string ReceiptApprove = "receipt.approve";   // duyệt phiếu → IsGhiNhanDongTien
    public const string ReceiptApprovalStart = "receipt.approval.start"; // khởi tạo luồng duyệt nhiều cấp
    public const string ReceiptApprovalAct = "receipt.approval.act";     // duyệt/từ chối 1 bước của luồng nhiều cấp

    public const string PaymentView = "payment.view";
    public const string PaymentCreate = "payment.create";
    public const string PaymentApprove = "payment.approve";   // duyệt phiếu → IsGhiNhanDongTien

    public const string ReportDebtView = "report.debt.view";  // báo cáo công nợ (CNPT hệ cũ)
    public const string ReportProviderDebtView = "report.providerdebt.view";  // công nợ phải trả NCC

    public const string ProviderView = "provider.view";
    public const string ProviderCreate = "provider.create";
    public const string ProviderUpdate = "provider.update";
    public const string ProviderDelete = "provider.delete";
    public const string CostView = "cost.view";
    public const string CostCreate = "cost.create";

    public const string CommissionView = "commission.view";
    public const string CommissionCreate = "commission.create";

    public const string SubscriptionView = "subscription.view";
    public const string SubscriptionManage = "subscription.manage";

    public const string MarketingView = "marketing.view";
    public const string MarketingCreate = "marketing.create";
    public const string MarketingSend = "marketing.send";

    public const string MarketView = "market.view";
    public const string MarketManage = "market.manage";

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
        (DepartureClose, "Booking"),
        (BookingView, "Booking"), (BookingCreate, "Booking"),
        (BookingSeatConfirm, "Booking"), (BookingSeatCancel, "Booking"),
        (ReceiptView, "Finance"), (ReceiptCreate, "Finance"), (ReceiptApprove, "Finance"),
        (ReceiptApprovalStart, "Finance"), (ReceiptApprovalAct, "Finance"),
        (PaymentView, "Finance"), (PaymentCreate, "Finance"), (PaymentApprove, "Finance"),
        (ReportDebtView, "Report"),
        (ReportProviderDebtView, "Report"),
        (ProviderView, "Provider"), (ProviderCreate, "Provider"),
        (ProviderUpdate, "Provider"), (ProviderDelete, "Provider"),
        (CostView, "Provider"), (CostCreate, "Provider"),
        (CommissionView, "Commission"), (CommissionCreate, "Commission"),
        (SubscriptionView, "Billing"), (SubscriptionManage, "Billing"),
        (MarketingView, "Marketing"), (MarketingCreate, "Marketing"), (MarketingSend, "Marketing"),
        (MarketView, "Catalog"), (MarketManage, "Catalog"),
    ];
}
