using TourKit.Application.Common;
using TourKit.Application.Content;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Content;

/// <summary>Test <see cref="PostCommentService"/> (legacy CommentsPost — bình luận/đánh giá bài viết).</summary>
public class PostCommentServiceTests
{
    private static PostCommentService NewService(out FakeRepository<PostComment> repo, out Post post)
    {
        repo = new FakeRepository<PostComment>();
        var postRepo = new FakeRepository<Post>();
        post = new Post { Title = "Tour Đà Nẵng", Slug = "tour-da-nang", Body = "..." };
        postRepo.AddAsync(post).GetAwaiter().GetResult();
        postRepo.SaveChangesAsync().GetAwaiter().GetResult();
        return new PostCommentService(repo, postRepo);
    }

    [Fact]
    public async Task CreateAsync_on_missing_post_throws()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateAsync(Guid.NewGuid(), new CreatePostCommentDto("A", "Hay", true)));
    }

    [Fact]
    public async Task CreateAsync_blank_author_or_content_throws()
    {
        var service = NewService(out _, out var post);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(post.Id, new CreatePostCommentDto(" ", "Hay", true)));
        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(post.Id, new CreatePostCommentDto("A", " ", true)));
    }

    [Fact]
    public async Task ListAsync_approvedOnly_filters_unapproved()
    {
        var service = NewService(out _, out var post);
        await service.CreateAsync(post.Id, new CreatePostCommentDto("A", "Đã duyệt", true));
        await service.CreateAsync(post.Id, new CreatePostCommentDto("B", "Chờ duyệt", false));

        Assert.Equal(2, (await service.ListAsync(post.Id, null)).Count);
        Assert.Single(await service.ListAsync(post.Id, true));
    }

    [Fact]
    public async Task SetApprovedAsync_toggles_flag()
    {
        var service = NewService(out _, out var post);
        var created = await service.CreateAsync(post.Id, new CreatePostCommentDto("A", "Tốt", false));

        await service.SetApprovedAsync(post.Id, created.Id, true);
        Assert.True((await service.ListAsync(post.Id, true)).Single().IsApproved);
    }

    [Fact]
    public async Task SetApprovedAsync_wrong_post_throws()
    {
        var service = NewService(out _, out var post);
        var created = await service.CreateAsync(post.Id, new CreatePostCommentDto("A", "Tốt", true));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.SetApprovedAsync(Guid.NewGuid(), created.Id, false));
    }

    [Fact]
    public async Task DeleteAsync_removes_comment()
    {
        var service = NewService(out _, out var post);
        var created = await service.CreateAsync(post.Id, new CreatePostCommentDto("A", "Tốt", true));

        await service.DeleteAsync(post.Id, created.Id);
        Assert.Empty(await service.ListAsync(post.Id, null));
    }
}
