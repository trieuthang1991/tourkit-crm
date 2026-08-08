using System.Security.Claims;
using TourKit.Api.Comments;

namespace TourKit.Api.Ai;

/// <summary>Kết quả soát quyền cho một thao tác AI trên bản ghi.</summary>
/// <param name="Error">Lý do từ chối, đã viết sẵn bằng tiếng Việt. <c>null</c> = cho qua.</param>
/// <param name="UserId">Người thực hiện; chỉ có nghĩa khi <paramref name="Error"/> là null.</param>
public sealed record AiAccessCheck(string? Error, Guid UserId)
{
    /// <summary>Cho qua.</summary>
    public static AiAccessCheck Allow(Guid userId) => new(null, userId);

    /// <summary>Từ chối kèm lý do.</summary>
    public static AiAccessCheck Deny(string reason) => new(reason, Guid.Empty);
}

/// <summary>
/// HÀNG RÀO của mọi thao tác AI đọc một bản ghi (chấm điểm, tóm tắt, soạn tin).
///
/// Tách thành hàm thuần — không đụng HTTP, không đụng DI — vì đây là chỗ quyết định ai được đọc gì,
/// và một quyết định như vậy phải kiểm thử được cạn kiệt. Nằm trong một hàm private của trang thì nó
/// chỉ được chạy qua khi có người mở trình duyệt.
///
/// Ba tầng, thiếu tầng nào cũng thủng:
///   1. Loại bản ghi phải nằm trong DANH SÁCH TRẮNG. Không có tầng này thì người dùng bịa tên loại
///      bất kỳ và biến endpoint thành cửa đọc dữ liệu tuỳ ý.
///   2. Người dùng phải có quyền XEM bản ghi cha. Kết quả AI kể lại chính nội dung bản ghi và cả
///      luồng trao đổi nội bộ — thường nhạy cảm hơn bản thân bản ghi.
///   3. Phải đọc được định danh người dùng, vì hạn mức tính theo người. Không có định danh thì từ
///      chối, chứ không cho qua rồi bỏ đếm — một đường vào không đếm được là một đường vào không
///      giới hạn.
/// </summary>
public static class AiRecordAccess
{
    /// <summary>Soát một yêu cầu. Dùng chung cho mọi handler AI trên bản ghi.</summary>
    public static AiAccessCheck Check(ClaimsPrincipal user, string? entityName, string? entityId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entry = CommentableEntities.Find(entityName);
        if (entry is null || string.IsNullOrWhiteSpace(entityId))
        {
            return AiAccessCheck.Deny("Không xử lý được loại bản ghi này.");
        }

        if (!user.HasClaim("perm", entry.ViewPermission))
        {
            return AiAccessCheck.Deny("Bạn không có quyền xem bản ghi này.");
        }

        var userId = ReadUserId(user);
        return userId is null
            ? AiAccessCheck.Deny("Phiên đăng nhập có vấn đề. Bạn đăng nhập lại nhé.")
            : AiAccessCheck.Allow(userId.Value);
    }

    /// <summary>Định danh người dùng: JWT dùng claim "sub", cookie dùng NameIdentifier.</summary>
    public static Guid? ReadUserId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
