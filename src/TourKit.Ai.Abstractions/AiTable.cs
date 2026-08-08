namespace TourKit.Ai.Abstractions;

/// <summary>
/// Một cột trong bảng kết quả.
/// </summary>
/// <param name="Label">Nhãn tiếng Việt hiện trên đầu cột.</param>
/// <param name="Type">
/// Cách hiển thị: <c>text</c>, <c>number</c>, <c>money</c>, <c>percent</c>. Giao diện dựa vào đây để
/// canh phải số và định dạng kiểu Việt Nam.
/// </param>
public sealed record AiColumn(string Label, string Type = "text");

/// <summary>
/// Bảng số liệu kèm theo câu trả lời.
///
/// Cột đi kèm NHÃN chứ không để giao diện đoán từ tên trường: đoán thì hoặc mất dấu tiếng Việt, hoặc
/// phải nhân bản một bảng ánh xạ tên trường sang nhãn ở phía JavaScript — và bảng đó sẽ lệch ngay lần
/// đầu có người thêm cột mới.
/// </summary>
/// <param name="Columns">Định nghĩa cột, theo đúng thứ tự các ô trong mỗi dòng.</param>
/// <param name="Rows">Các dòng; mỗi dòng có số ô bằng số cột.</param>
public sealed record AiTable(IReadOnlyList<AiColumn> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows);
