namespace TourKit.Ai.Abstractions;

/// <summary>
/// Danh mục TÍNH NĂNG AI của hệ thống. Cấu hình đi theo tính năng chứ không theo nhà cung cấp: mỗi
/// tính năng tự khai báo dùng hãng nào, model nào. Nhờ vậy đổi model cho riêng phần chấm điểm khách
/// hàng là sửa một dòng cấu hình, không ảnh hưởng trợ lý tra cứu.
///
/// Tên tính năng là HỢP ĐỒNG với file cấu hình — đổi hằng số ở đây mà quên sửa appsettings sẽ bị
/// <see cref="AiOptions.Validate"/> bắt lỗi lúc khởi động.
/// </summary>
public static class AiFeatures
{
    /// <summary>Trợ lý tra cứu tiếng Việt trong ứng dụng (giai đoạn 1) — cần gọi công cụ.</summary>
    public const string Assistant = "Assistant";

    /// <summary>Soạn sẵn nội dung để người dùng sửa rồi mới lưu (phiếu chăm sóc, tin nhắn, mô tả tour).</summary>
    public const string Draft = "Draft";

    /// <summary>Phân loại / gán nhãn nhanh — việc đơn giản, nên trỏ vào model rẻ nhất.</summary>
    public const string Classify = "Classify";

    /// <summary>Tóm tắt một bản ghi nghiệp vụ (diễn biến đơn hàng, luồng bình luận).</summary>
    public const string Summarize = "Summarize";

    /// <summary>Chấm điểm khách hàng và cơ hội bán hàng — cần suy luận, nên dùng model mạnh.</summary>
    public const string Scoring = "Scoring";

    /// <summary>Nhúng vector cho kho tài liệu quy trình (giai đoạn 3).</summary>
    public const string Embedding = "Embedding";

    /// <summary>Đọc giấy tờ khách gửi vào (OCR) — KHÔNG cần LLM, gọi thẳng dịch vụ OCR.</summary>
    public const string DocumentRead = "DocumentRead";

    /// <summary>Chatbot cho khách hàng cuối (giai đoạn 4) — chạy tiến trình riêng, bộ công cụ hẹp.</summary>
    public const string CustomerChat = "CustomerChat";

    private static readonly Dictionary<string, string> Catalog =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Assistant] = "Trợ lý tra cứu",
            [Draft] = "Soạn sẵn nội dung",
            [Classify] = "Phân loại nhanh",
            [Summarize] = "Tóm tắt bản ghi",
            [Scoring] = "Chấm điểm khách hàng và cơ hội",
            [Embedding] = "Nhúng vector cho tra cứu tài liệu",
            [DocumentRead] = "Đọc giấy tờ (OCR)",
            [CustomerChat] = "Chatbot khách hàng",
        };

    /// <summary>Toàn bộ tên tính năng hợp lệ, theo thứ tự lộ trình triển khai.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Assistant, Draft, Classify, Summarize, Scoring, Embedding, DocumentRead, CustomerChat,
    ];

    /// <summary>
    /// Những tính năng ĐÃ CÓ CODE chạy. Các tên còn lại trong <see cref="All"/> mới chỉ có chỗ trong
    /// cấu hình — hình dạng đã chốt nhưng chưa viết.
    ///
    /// Danh sách này tồn tại vì cấu hình không tự biết nó có được ai dùng hay không: đặt
    /// <c>Draft:Enabled = true</c> lúc chưa viết Draft thì hệ thống vẫn khởi động, vẫn đòi khoá cho
    /// nhà cung cấp của nó, và người đọc appsettings tưởng tính năng đang chạy. Viết xong tính năng
    /// nào thì thêm tên nó vào đây — đúng một dòng.
    /// </summary>
    public static IReadOnlySet<string> Implemented { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Assistant, Scoring };

    /// <summary>Tính năng này đã có code chạy chưa.</summary>
    public static bool IsImplemented(string name) => Implemented.Contains(name);

    /// <summary>Tên này có phải một tính năng đã biết không (so sánh không phân biệt hoa thường).</summary>
    public static bool IsKnown(string name) => Catalog.ContainsKey(name);

    /// <summary>Nhãn tiếng Việt để đưa vào thông báo lỗi và màn hình quản trị.</summary>
    public static string Label(string name) =>
        Catalog.TryGetValue(name, out var label) ? label : name;
}
