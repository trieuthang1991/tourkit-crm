namespace TourKit.Application.Collaboration;

/// <summary>
/// Bình luận trên bản ghi nghiệp vụ bất kỳ (cơ hội, khách hàng, đơn hàng…).
///
/// PHÂN QUYỀN KHÔNG NẰM Ở ĐÂY. Service không biết "Lead" cần quyền gì — tầng API kiểm tra quyền của
/// entity CHA trước khi gọi xuống (xem <c>TourKit.Api.Comments.CommentableEntities</c>). Đặt luật
/// quyền ở đây sẽ buộc tầng Application phải biết danh mục quyền của tầng Api, đảo chiều phụ thuộc.
/// </summary>
public interface IEntityCommentService
{
    /// <summary>
    /// Bình luận của một bản ghi, mới nhất trước. Luôn có biên (<paramref name="take"/>) — một cơ hội
    /// đã chăm sóc lâu có thể có hàng trăm bình luận, và luồng này còn được trợ lý AI đọc.
    /// </summary>
    Task<IReadOnlyList<EntityCommentDto>> ListAsync(string entityName, string entityId, int take = 50);

    /// <summary>Tổng số bình luận — để giao diện hiện "còn N bình luận cũ hơn" khi bị cắt.</summary>
    Task<int> CountAsync(string entityName, string entityId);

    Task<EntityCommentDto> CreateAsync(CreateEntityCommentDto dto);

    /// <summary>Chỉ TÁC GIẢ xoá được. Không có sửa ở v1 — xem ghi chú trong cài đặt.</summary>
    Task DeleteAsync(Guid id);
}
