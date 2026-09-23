namespace TourKit.Shared.Entities;

/// <summary>
/// Chiến dịch chia số Sale (legacy "Chia số Sale"): gom một tập lead/data khách và phân bổ cho sale,
/// theo dõi tiến độ chăm sóc + tỷ lệ chốt. Lead thuộc chiến dịch qua <see cref="Lead.CampaignId"/>.
/// </summary>
public sealed class LeadCampaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// Mã ngắn cho NGƯỜI đọc, dạng <c>CD-2026-007</c>. Duy nhất theo công ty, tự sinh khi tạo.
    ///
    /// Tồn tại vì người dựng form thu lead phải nhúng chiến dịch vào form đó bằng tay. Bắt họ chép
    /// một GUID <c>3f2a8c14-…</c> là cầm chắc sai một ký tự mà không ai phát hiện ra — lead vẫn vào,
    /// chỉ là rơi vào hư không. Mã ngắn thì đọc được, gõ lại được, và nhìn là biết của năm nào.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }   // Người tạo chiến dịch
    public int Status { get; set; }              // 0 đang chạy, 1 hoàn thành
    public string? Note { get; set; }

    /// <summary>
    /// Cách chia số cho nhóm nhân viên của chiến dịch — xem <see cref="Enums.LeadAssignMode"/>.
    ///
    /// Đây mới là phần làm cho màn "Chia số Sale" đúng với tên của nó: lead về từ chiến dịch được
    /// GIAO TỰ ĐỘNG cho một người trong nhóm. Trước đây chiến dịch chỉ là một cái nhãn không ai
    /// gắn, nên mọi con số của màn luôn bằng 0.
    /// </summary>
    public int AssignMode { get; set; }

    /// <summary>
    /// Nhóm nhân viên nhận số, dạng JSON: mảng id ĐÃ SẮP theo đúng thứ tự vòng chia.
    ///
    /// Để JSON chứ không dựng bảng nối là theo đúng luật của repo: chỉ thứ cần LỌC hoặc SẮP XẾP ở
    /// SQL mới phải thành cột thật. Nhóm này luôn được đọc TRỌN VẸN cho một chiến dịch để chọn ra
    /// một người — không có truy vấn nào lọc theo thành viên, và báo cáo phân bổ thì gom theo
    /// <c>Lead.AssignedToUserId</c> chứ không theo nhóm. Cùng khuôn với
    /// <c>CustomerCrmProfile.AssignedTo</c> đã có sẵn.
    ///
    /// Thứ tự mảng CHÍNH LÀ thứ tự vòng chia, nên không cần cột thứ tự riêng.
    /// </summary>
    public string? AssigneesJson { get; set; }
}
