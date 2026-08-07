using TourKit.Api.Authz;

namespace TourKit.Api.Comments;

/// <summary>
/// DANH SÁCH TRẮNG các bản ghi được phép bình luận, kèm mã quyền phải có để XEM bản ghi đó.
///
/// Đây là hàng rào an toàn của tính năng bình luận. Endpoint bình luận dùng chung cho mọi loại bản
/// ghi, nên nếu không tra bảng này thì:
///   1. Ai cũng đọc được bình luận của bản ghi mình không có quyền xem — chỉ cần đoán đúng tên loại
///      và id. Bình luận thường chứa nhiều thông tin nhạy cảm hơn chính bản ghi.
///   2. Người dùng bịa ra tên loại bất kỳ và biến bảng bình luận thành nơi lưu dữ liệu tuỳ ý.
///
/// Quyền bình luận KẾ THỪA quyền xem bản ghi cha: xem được cơ hội thì đọc và viết được bình luận
/// của cơ hội đó. Không đặt mã quyền riêng cho bình luận — hai bộ quyền lệch nhau sớm muộn sẽ cho
/// ra tổ hợp "không xem được bản ghi nhưng đọc được bình luận của nó".
/// </summary>
public static class CommentableEntities
{
    /// <param name="Label">Tên hiển thị, dùng trong thông báo "@ nhắc bạn trong {Label}".</param>
    /// <param name="ViewPermission">Mã quyền phải có để xem bản ghi cha.</param>
    /// <param name="LinkTemplate">Route để người nhận thông báo bấm vào; <c>{id}</c> được thay bằng khoá bản ghi.</param>
    public sealed record Entry(string Label, string ViewPermission, string LinkTemplate);

    private static readonly IReadOnlyDictionary<string, Entry> Map =
        new Dictionary<string, Entry>(StringComparer.Ordinal)
        {
            ["Lead"] = new("Cơ hội bán hàng", Permissions.LeadView, "/co-hoi"),
            ["Customer"] = new("Khách hàng", Permissions.CustomerView, "/khach-hang/{id}"),
            ["Order"] = new("Đơn hàng", Permissions.BookingView, "/don-hang"),
        };

    /// <summary>Tra loại bản ghi. null = không nằm trong danh sách trắng → từ chối, KHÔNG mặc định cho qua.</summary>
    public static Entry? Find(string? entityName) =>
        string.IsNullOrWhiteSpace(entityName) ? null : Map.GetValueOrDefault(entityName.Trim());

    public static string LinkFor(Entry entry, string entityId) =>
        entry.LinkTemplate.Replace("{id}", Uri.EscapeDataString(entityId), StringComparison.Ordinal);
}
