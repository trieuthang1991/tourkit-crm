namespace TourKit.Shared.Entities;

/// <summary>
/// Một CỘT trong phễu Cơ hội bán hàng (legacy <c>SectionWork</c> — các mục của một Workflow).
///
/// Hệ cũ KHÔNG cứng hoá danh sách trạng thái: ô trạng thái ở màn sửa phiếu đọc thẳng từ bảng này
/// (<c>sectionBoard</c>), nên mỗi công ty tự đặt tên và thứ tự các bước bán hàng của mình.
///
/// Nhưng <see cref="Code"/> thì KHÔNG tự do hoàn toàn: hai mã dưới đây bị luật nghiệp vụ khoá cứng
/// (xem <see cref="OpportunityStageCode"/>) — huỷ thì bắt buộc có lý do, chốt đơn thì do luồng đặt
/// chỗ đánh dấu. Cho người dùng đổi số của hai cột đó là làm hỏng cả hai luật mà không có gì báo.
/// </summary>
public sealed class OpportunityStage : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Mã trạng thái lưu vào <see cref="SalesOpportunity.StageCode"/>. Duy nhất theo tenant.</summary>
    public int Code { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Màu chữ của thẻ trên bảng kanban (legacy <c>TextColor</c>), dạng #rrggbb.</summary>
    public string? Color { get; set; }

    /// <summary>Cột hệ thống: không cho đổi mã, không cho xoá (legacy AllowUpdate/AllowDelete).</summary>
    public bool IsSystem { get; set; }

    public int Status { get; set; }
}

/// <summary>
/// Hai mã cột mà LUẬT bám vào. Mọi mã khác là bước trung gian do người dùng tự định nghĩa, hệ thống
/// không diễn giải gì thêm.
///
/// Giữ đúng số của hệ cũ (<c>BookingStatusDefault</c> trong ServiceTypeEnum.js) để dữ liệu chuyển
/// sang không phải ánh xạ lại: 1 Tạo mới · 2 Chờ xử lý · 3 Đang xử lý · 4 Đã xử lý · 5 Huỷ · 6 Chốt đơn.
/// </summary>
public static class OpportunityStageCode
{
    public const int TaoMoi = 1;
    public const int ChoXuLy = 2;
    public const int DangXuLy = 3;
    public const int DaXuLy = 4;

    /// <summary>Huỷ — chuyển sang đây BẮT BUỘC kèm lý do (legacy DetailReasonSwitch).</summary>
    public const int Huy = 5;

    /// <summary>Chốt đơn — KHÔNG đặt tay, do luồng đặt chỗ đánh dấu khi sinh đơn từ cơ hội.</summary>
    public const int ChotDon = 6;

    /// <summary>Bộ cột mặc định khi tạo tenant mới, bám đúng hệ cũ.</summary>
    public static readonly (int Code, string Name, bool IsSystem)[] MacDinh =
    [
        (TaoMoi, "Tạo mới", false),
        (ChoXuLy, "Chờ xử lý", false),
        (DangXuLy, "Đang xử lý", false),
        (DaXuLy, "Đã xử lý", false),
        (Huy, "Huỷ", true),
        (ChotDon, "Chốt đơn", true),
    ];
}
