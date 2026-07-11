using TourKit.Application.Common;
using TourKit.Application.Content;
using TourKit.Application.Content.Validators;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Content;

/// <summary>Test <see cref="PostService"/> + <see cref="PostCategoryService"/> (CMS bài viết).</summary>
public class PostServiceTests
{
    private static PostService NewPosts(out FakeRepository<Post> repo, out FakeRepository<PostCategory> catRepo)
    {
        repo = new FakeRepository<Post>();
        catRepo = new FakeRepository<PostCategory>();
        return new PostService(repo, catRepo, new CreatePostValidator(), new UpdatePostValidator());
    }

    private static PostCategoryService NewCategories(out FakeRepository<PostCategory> repo)
    {
        repo = new FakeRepository<PostCategory>();
        return new PostCategoryService(repo, new CreatePostCategoryValidator(), new UpdatePostCategoryValidator());
    }

    private static CreatePostDto NewDto(string title = "Khuyến mãi hè", string slug = "khuyen-mai-he", int status = 0, Guid? cat = null) =>
        new(title, slug, "Tóm tắt", "Nội dung bài viết.", cat, status);

    [Fact]
    public async Task CreateAsync_draft_has_no_published_at()
    {
        var service = NewPosts(out _, out _);

        var dto = await service.CreateAsync(NewDto(status: 0));

        Assert.Equal(0, dto.Status);
        Assert.Null(dto.PublishedAt);
    }

    [Fact]
    public async Task CreateAsync_published_sets_published_at()
    {
        var service = NewPosts(out _, out _);

        var dto = await service.CreateAsync(NewDto(status: 1));

        Assert.Equal(1, dto.Status);
        Assert.NotNull(dto.PublishedAt);
    }

    [Fact]
    public async Task UpdateAsync_publish_then_unpublish_toggles_published_at()
    {
        var service = NewPosts(out _, out _);
        var created = await service.CreateAsync(NewDto(status: 0));

        await service.UpdateAsync(created.Id, new UpdatePostDto("T", "khuyen-mai-he", null, "Body", null, 1));
        Assert.NotNull((await service.GetAsync(created.Id)).PublishedAt);

        await service.UpdateAsync(created.Id, new UpdatePostDto("T", "khuyen-mai-he", null, "Body", null, 0));
        Assert.Null((await service.GetAsync(created.Id)).PublishedAt);
    }

    [Fact]
    public async Task CreateAsync_duplicate_slug_throws()
    {
        var service = NewPosts(out _, out _);
        await service.CreateAsync(NewDto(slug: "tin-1"));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(title: "Khác", slug: "TIN-1")));
    }

    [Fact]
    public async Task CreateAsync_resolves_category_name_and_validates_existence()
    {
        var service = NewPosts(out _, out var catRepo);
        var cat = new PostCategory { Name = "Cẩm nang", Slug = "cam-nang" };
        await catRepo.AddAsync(cat);
        await catRepo.SaveChangesAsync();

        var dto = await service.CreateAsync(NewDto(cat: cat.Id));
        Assert.Equal("Cẩm nang", dto.CategoryName);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(slug: "x", cat: Guid.NewGuid())));
    }

    [Fact]
    public async Task ListAsync_filters_by_status()
    {
        var service = NewPosts(out _, out _);
        await service.CreateAsync(NewDto(slug: "a", status: 1));
        await service.CreateAsync(NewDto(slug: "b", status: 0));

        var published = await service.ListAsync(null, 1);
        Assert.Single(published);
    }

    [Fact]
    public async Task Category_create_duplicate_slug_throws()
    {
        var service = NewCategories(out _);
        await service.CreateAsync(new CreatePostCategoryDto("Tin tức", "tin-tuc", 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreatePostCategoryDto("Khác", "tin-tuc", 2)));
    }
}
