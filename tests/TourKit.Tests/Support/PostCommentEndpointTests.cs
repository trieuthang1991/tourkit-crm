using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Content;

namespace TourKit.Tests.Support;

/// <summary>Bình luận/đánh giá bài viết (legacy CommentsPost) qua /api/v1/posts/{postId}/comments.</summary>
public class PostCommentEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public PostCommentEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_list_approve_and_delete_comment()
    {
        var client = await LoggedInClientAsync("post-comment-a");
        var post = await (await client.PostAsJsonAsync("/api/v1/posts",
            new { Title = "Cẩm nang Đà Lạt", Slug = "cam-nang-da-lat", Summary = (string?)null, Body = "Nội dung", CategoryId = (Guid?)null, Status = 1 }))
            .Content.ReadFromJsonAsync<PostDto>();

        var created = await (await client.PostAsJsonAsync($"/api/v1/posts/{post!.Id}/comments",
            new CreatePostCommentDto("Trần Thị B", "Bài viết rất hữu ích!", false)))
            .Content.ReadFromJsonAsync<PostCommentDto>();
        Assert.False(created!.IsApproved);

        // approvedOnly lọc bỏ bình luận chưa duyệt
        var pending = await client.GetFromJsonAsync<List<PostCommentDto>>($"/api/v1/posts/{post.Id}/comments?approvedOnly=true");
        Assert.Empty(pending!);

        var approve = await client.PostAsync($"/api/v1/posts/{post.Id}/comments/{created.Id}/approve", null);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        var approved = await client.GetFromJsonAsync<List<PostCommentDto>>($"/api/v1/posts/{post.Id}/comments?approvedOnly=true");
        Assert.Single(approved!);

        var del = await client.DeleteAsync($"/api/v1/posts/{post.Id}/comments/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var empty = await client.GetFromJsonAsync<List<PostCommentDto>>($"/api/v1/posts/{post.Id}/comments");
        Assert.Empty(empty!);
    }
}
