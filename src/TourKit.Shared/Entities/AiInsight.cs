namespace TourKit.Shared.Entities;

/// <summary>
/// Một lần AI chấm điểm / tóm tắt / soạn tin cho một bản ghi nghiệp vụ.
///
/// Trước đây kết quả chỉ trả về trình duyệt rồi biến mất khi tải lại trang. Người dùng mở lại hồ sơ
/// hôm sau là thẻ AI trống trơn, muốn xem lại phải chấm lại — mất thêm 15-20 giây và thêm một lượt
/// gọi model có tính phí. Tệ hơn: điểm hôm nay và điểm tuần trước không so được với nhau, mà đó mới
/// là giá trị thật của việc chấm điểm — thấy khách đang ấm lên hay nguội đi.
///
/// GIỮ LỊCH SỬ, không ghi đè: mỗi lần bấm là một dòng mới. Ghi đè thì lại mất đúng khả năng so sánh
/// theo thời gian vừa nói. Màn hình đọc dòng mới nhất, ai muốn xem lại thì lật các dòng cũ.
///
/// Dùng chung cặp khoá <c>(EntityName, EntityId)</c> với <see cref="EntityComment"/> và
/// <see cref="ActivityLog"/> — cùng một quy ước "gắn vào bản ghi bất kỳ", ghép được vào cùng dòng
/// thời gian mà không phải dịch khoá qua lại.
/// </summary>
public sealed class AiInsight : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Tên type entity nghiệp vụ — cùng quy ước với <see cref="ActivityLog.EntityName"/>.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Khoá bản ghi — cùng quy ước với <see cref="ActivityLog.EntityId"/>.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Việc AI đã làm: <c>Review</c> (chấm điểm) · <c>Summary</c> (tóm tắt) · <c>Draft</c> (soạn tin).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Người đã bấm. Không nullable: mọi kết quả đều do một người yêu cầu, không có đường tự động.</summary>
    public Guid UserId { get; set; }

    /// <summary>Nội dung văn bản của tóm tắt / tin nhắn soạn sẵn. Null với bản chấm điểm.</summary>
    public string? Text { get; set; }

    /// <summary>Điểm tổng (0-100) và nhóm xếp hạng. Null với tóm tắt / soạn tin.</summary>
    public int? Score { get; set; }

    public string? Band { get; set; }

    /// <summary>Nhận định ngắn kèm bản chấm điểm.</summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Bảng tiêu chí + rủi ro + việc nên làm, lưu JSON.
    ///
    /// Để JSON thay vì tách bảng con vì đây là dữ liệu chỉ đọc kèm đúng một lần chấm: không bao giờ
    /// truy vấn ngược kiểu "tiêu chí này bị điểm thấp ở những hồ sơ nào", và bộ tiêu chí còn đổi theo
    /// ai-scoring.json nên tách bảng sẽ phải migrate mỗi lần đổi luật chấm.
    /// </summary>
    public string? DetailJson { get; set; }
}
