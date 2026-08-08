using Microsoft.Extensions.AI;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// Một "công cụ" trợ lý được phép gọi. Mỗi công cụ bọc ĐÚNG MỘT hàm nghiệp vụ đã có ở tầng Application
/// — nhờ vậy bộ lọc tenant, ẩn bản ghi đã xoá và phân quyền của hàm đó tự động áp dụng, model không có
/// đường nào đi vòng qua.
///
/// Tên, mô tả và schema tham số nằm trong <see cref="Function"/> và được sinh tự động từ chữ ký hàm —
/// không phải viết tay JSON Schema, nên schema không bao giờ lệch khỏi code.
/// </summary>
public interface IAiTool
{
    /// <summary>Khai báo gửi cho model: tên, mô tả tiếng Việt, schema tham số.</summary>
    AIFunction Function { get; }

    /// <summary>
    /// Mã quyền cần có để THẤY công cụ này (vd <c>report.turnover.view</c>); <c>null</c> = ai đăng nhập
    /// cũng thấy. Đây là hàng rào thật: công cụ không nằm trong danh sách gửi cho model thì model không
    /// có tên để gọi, kể cả khi người dùng cố dụ.
    /// </summary>
    string? RequiredPermission { get; }
}

/// <summary>
/// Kết quả một lần chạy công cụ, tách làm hai phần: <paramref name="Text"/> cho model đọc và diễn đạt
/// lại, <paramref name="Data"/> cho giao diện vẽ bảng. Nhờ vậy câu trả lời không phải một đoạn văn kể
/// số mà là bảng thật người dùng kiểm chứng được ngay.
/// </summary>
/// <param name="Text">Tóm tắt dạng văn bản để model đọc.</param>
/// <param name="Data">Dữ liệu có cấu trúc để giao diện vẽ. Khi đọc lại sau khi chạy sẽ là JsonElement.</param>
/// <param name="LinkUrl">Đường dẫn màn hình tương ứng để người dùng bấm sang xem đầy đủ.</param>
public sealed record AiToolResult(string Text, object? Data = null, string? LinkUrl = null);
