using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Content;

namespace TourKit.Api.Controllers;

/// <summary>Bình luận/đánh giá bài viết (legacy CommentsPost) dưới /api/v1/posts/{postId}/comments.</summary>
[ApiController]
[Route("api/v1/posts/{postId:guid}/comments")]
public sealed class PostCommentsController(IPostCommentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.PostView)]
    public async Task<IActionResult> List(Guid postId, [FromQuery] bool? approvedOnly)
        => Ok(await service.ListAsync(postId, approvedOnly));

    [HttpPost]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Create(Guid postId, [FromBody] CreatePostCommentDto dto)
    {
        var created = await service.CreateAsync(postId, dto);
        return Created($"/api/v1/posts/{postId}/comments/{created.Id}", created);
    }

    [HttpPost("{commentId:guid}/approve")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Approve(Guid postId, Guid commentId)
    {
        await service.SetApprovedAsync(postId, commentId, true);
        return NoContent();
    }

    [HttpPost("{commentId:guid}/unapprove")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Unapprove(Guid postId, Guid commentId)
    {
        await service.SetApprovedAsync(postId, commentId, false);
        return NoContent();
    }

    [HttpDelete("{commentId:guid}")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Delete(Guid postId, Guid commentId)
    {
        await service.DeleteAsync(postId, commentId);
        return NoContent();
    }
}
