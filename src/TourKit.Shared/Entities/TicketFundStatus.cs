namespace TourKit.Shared.Entities;

/// <summary>
/// Trạng thái sử dụng của một quỹ vé ứng — bám legacy <c>StatusTicketFund</c> (New = 0, Used = 1).
/// Chỉ hai bậc: vé mới cấp còn trong quỹ, hay đã xuất/dùng cho khách. Đây là TRỤC KHÁC với "đóng
/// quỹ" (<see cref="TicketFund.IsClosed"/>) — một vé đã dùng vẫn có thể chưa đóng quỹ và ngược lại.
///
/// Giá trị vẫn LƯU dưới dạng <see cref="int"/> (cột không đổi, không cần migration). Enum này là
/// nguồn chuẩn cho nhãn hiển thị + ô chọn ở giao diện; dữ liệu legacy có thể mang giá trị ngoài
/// {0,1} nên nơi hiển thị phải chịu được mã lạ thay vì ném lỗi.
/// </summary>
public enum TicketFundStatus
{
    /// <summary>Chưa sử dụng — vé còn trong quỹ, chưa gán cho khách (legacy New = 0).</summary>
    ChuaSuDung = 0,

    /// <summary>Đã sử dụng — vé đã xuất/dùng cho đơn (legacy Used = 1).</summary>
    DaSuDung = 1,
}
