
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>Khách tiềm năng (phễu bán). Convert thành Customer khi "Won".</summary>
public sealed class Lead : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Source { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Guid? AssignedToUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }      // Người tạo lead
    public Guid? BranchId { get; set; }             // Chi nhánh (legacy ChiNhanh)
    public Guid? CampaignId { get; set; }           // Chiến dịch chia số (Chia số Sale) — lead thuộc chiến dịch nào
    public Guid? ConvertedCustomerId { get; set; }

    /// <summary>
    /// Nhu cầu khách tự nêu, bằng lời của họ: "đi Đà Nẵng 4 ngày tháng 10, 2 người lớn 1 trẻ".
    ///
    /// Trước đây không có chỗ nào giữ thứ này, nên một lead chỉ còn tên với số điện thoại — người
    /// gọi lại phải hỏi lại từ đầu, và phần chấm điểm AI luôn kết luận "hồ sơ gần như trống" vì
    /// đúng là nó trống thật. Đây là trường quyết định tư vấn đúng hay sai.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Nguồn CHI TIẾT dạng JSON (utm_source/medium/campaign…, trang đích, referrer) — xem
    /// <c>LeadAttribution</c>.
    ///
    /// Tách khỏi <see cref="Source"/> là có chủ ý: <see cref="Source"/> giữ GIÁ TRỊ CHUẨN lấy từ
    /// danh mục để gộp báo cáo ở SQL, còn ở đây là phần mở rộng tuỳ ý. Nhồi "utm_source=zns" thẳng
    /// vào <see cref="Source"/> thì "Zalo", "zalo", "utm_source=zns" thành ba nguồn riêng và mọi
    /// báo cáo theo nguồn vỡ theo — đúng thứ danh mục sinh ra để ngăn.
    /// </summary>
    public string? AttributionJson { get; set; }
}
