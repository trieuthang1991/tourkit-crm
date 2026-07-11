using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Content;

/// <summary>
/// Bình luận/đánh giá bài viết (legacy CommentsPost). Nhân viên nhập/duyệt (testimonial biên tập);
/// cổng public tự gửi là follow-up. Duyệt (<see cref="PostComment.IsApproved"/>) để lọc hiển thị.
/// </summary>
public sealed class PostCommentService(
    IRepository<PostComment> repo,
    IRepository<Post> postRepo) : IPostCommentService
{
    public async Task<IReadOnlyList<PostCommentDto>> ListAsync(Guid postId, bool? approvedOnly)
    {
        var items = await repo.ListAsync(c =>
            c.PostId == postId &&
            (approvedOnly != true || c.IsApproved));
        return items.OrderByDescending(c => c.CreatedAt).Select(Map).ToList();
    }

    public async Task<PostCommentDto> CreateAsync(Guid postId, CreatePostCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AuthorName))
        {
            throw new ValidationAppException("Tên người bình luận không được trống.");
        }

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new ValidationAppException("Nội dung bình luận không được trống.");
        }

        _ = await postRepo.GetByIdAsync(postId) ?? throw new NotFoundException();

        var entity = new PostComment
        {
            PostId = postId,
            AuthorName = dto.AuthorName.Trim(),
            Content = dto.Content.Trim(),
            IsApproved = dto.IsApproved,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task SetApprovedAsync(Guid postId, Guid commentId, bool approved)
    {
        var entity = await GetOwnedAsync(postId, commentId);
        entity.IsApproved = approved;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid postId, Guid commentId)
    {
        var entity = await GetOwnedAsync(postId, commentId);
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task<PostComment> GetOwnedAsync(Guid postId, Guid commentId)
    {
        var entity = await repo.GetByIdAsync(commentId) ?? throw new NotFoundException();
        if (entity.PostId != postId)
        {
            throw new NotFoundException();
        }

        return entity;
    }

    private static PostCommentDto Map(PostComment c) => new(
        c.Id, c.PostId, c.AuthorName, c.Content, c.IsApproved, c.CreatedAt);
}
