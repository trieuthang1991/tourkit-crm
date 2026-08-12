namespace TourKit.Shared.Entities;

/// <summary>
/// Một người phụ trách hoặc người theo dõi của một cơ hội.
///
/// Vì sao là BẢNG RIÊNG chứ không phải cột chuỗi: hệ cũ nhét danh sách id vào một cột text
/// (<c>NguoiPhuTrachs</c>, <c>IdsFollower</c> — dạng "3,17,42"). Cách đó hỏng ở ba chỗ, và cả ba đều
/// là thứ màn Cơ hội cần dùng hằng ngày:
///
/// - Lọc "cơ hội của tôi" phải viết <c>LIKE '%,17,%'</c> — quét toàn bảng, không index nào đỡ được,
///   và còn khớp nhầm: id 17 khớp cả chuỗi chứa 170.
/// - Không có khoá ngoại, nên nhân viên nghỉ việc bị xoá thì cột kia vẫn giữ id trỏ vào hư không.
/// - Không đếm được "mỗi người đang giữ bao nhiêu cơ hội" bằng một câu GROUP BY.
///
/// Người phụ trách và người theo dõi dùng CHUNG bảng này, phân biệt bằng <see cref="IsFollower"/>:
/// hai vai chỉ khác nhau ở quyền, còn mọi truy vấn ("ai dính tới cơ hội này") đều muốn cả hai.
/// </summary>
public sealed class SalesOpportunityAssignee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>false = người phụ trách (làm), true = người theo dõi (chỉ nhận thông báo).</summary>
    public bool IsFollower { get; set; }
}
