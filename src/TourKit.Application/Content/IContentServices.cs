namespace TourKit.Application.Content;

public interface IPostCategoryService
{
    Task<IReadOnlyList<PostCategoryDto>> ListAsync();
    Task<PostCategoryDto> CreateAsync(CreatePostCategoryDto dto);
    Task UpdateAsync(Guid id, UpdatePostCategoryDto dto);
    Task DeleteAsync(Guid id);
}

public interface IPostService
{
    Task<IReadOnlyList<PostDto>> ListAsync(Guid? categoryId, int? status);
    Task<PostDto> GetAsync(Guid id);
    Task<PostDto> CreateAsync(CreatePostDto dto);
    Task UpdateAsync(Guid id, UpdatePostDto dto);
    Task DeleteAsync(Guid id);
}

public interface IPostCommentService
{
    Task<IReadOnlyList<PostCommentDto>> ListAsync(Guid postId, bool? approvedOnly);
    Task<PostCommentDto> CreateAsync(Guid postId, CreatePostCommentDto dto);
    Task SetApprovedAsync(Guid postId, Guid commentId, bool approved);
    Task DeleteAsync(Guid postId, Guid commentId);
}
