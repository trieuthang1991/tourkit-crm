namespace TourKit.Application.Collaboration;

// CanDelete: người đang xem có xoá được bình luận này không (chỉ tác giả mới xoá được lời của mình).
public sealed record EntityCommentDto(
    Guid Id,
    string EntityName,
    string EntityId,
    Guid UserId,
    string AuthorName,
    string Content,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTimeOffset CreatedAt,
    bool CanDelete);

/// <summary>
/// Tạo bình luận. <paramref name="LinkUrl"/> và <paramref name="EntityLabel"/> do TẦNG API truyền
/// xuống chứ không do service tự đoán: chỉ tầng API biết route của màn hình, tầng Application không
/// được biết đường dẫn giao diện.
/// </summary>
public sealed record CreateEntityCommentDto(
    string EntityName,
    string EntityId,
    string Content,
    IReadOnlyList<Guid>? MentionedUserIds = null,
    string? LinkUrl = null,
    string? EntityLabel = null);
