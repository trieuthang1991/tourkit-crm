using FluentValidation;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Content;

/// <summary>
/// Bài viết/tin tức (legacy Posts) — CRUD + lọc theo chuyên mục/trạng thái. Slug duy nhất/tenant;
/// khi chuyển sang xuất bản (Status=1) mà chưa có PublishedAt thì đặt = thời điểm hiện tại (một chỗ).
/// </summary>
public sealed class PostService(
    IRepository<Post> repo,
    IRepository<PostCategory> categoryRepo,
    IValidator<CreatePostDto> createValidator,
    IValidator<UpdatePostDto> updateValidator) : IPostService
{
    private const int Published = 1;

    public async Task<IReadOnlyList<PostDto>> ListAsync(Guid? categoryId, int? status)
    {
        var items = await repo.ListAsync(p =>
            (categoryId == null || p.CategoryId == categoryId) &&
            (status == null || p.Status == status));
        var names = await LoadCategoryNamesAsync();
        return items.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt).Select(p => Map(p, names)).ToList();
    }

    public async Task<PostDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return Map(entity, await LoadCategoryNamesAsync());
    }

    public async Task<PostDto> CreateAsync(CreatePostDto dto)
    {
        await Validate(createValidator, dto);
        var slug = dto.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUnique(slug, null);
        await EnsureCategoryAsync(dto.CategoryId);

        var entity = new Post
        {
            Title = dto.Title.Trim(),
            Slug = slug,
            Summary = dto.Summary?.Trim(),
            Body = dto.Body,
            CategoryId = dto.CategoryId,
            Status = dto.Status,
            PublishedAt = dto.Status == Published ? DateTimeOffset.UtcNow : null,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity, await LoadCategoryNamesAsync());
    }

    public async Task UpdateAsync(Guid id, UpdatePostDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var slug = dto.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUnique(slug, id);
        await EnsureCategoryAsync(dto.CategoryId);

        entity.Title = dto.Title.Trim();
        entity.Slug = slug;
        entity.Summary = dto.Summary?.Trim();
        entity.Body = dto.Body;
        entity.CategoryId = dto.CategoryId;
        entity.Status = dto.Status;
        entity.LikeCount = dto.LikeCount;
        // Lần đầu xuất bản → ghi thời điểm; gỡ xuất bản (về nháp) → xoá thời điểm.
        if (dto.Status == Published)
        {
            entity.PublishedAt ??= DateTimeOffset.UtcNow;
        }
        else
        {
            entity.PublishedAt = null;
        }

        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task EnsureSlugUnique(string slug, Guid? excludeId)
    {
        if (await repo.AnyAsync(x => x.Slug == slug && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Slug \"{slug}\" đã tồn tại.");
        }
    }

    private async Task EnsureCategoryAsync(Guid? categoryId)
    {
        if (categoryId is { } cid && !await categoryRepo.AnyAsync(c => c.Id == cid))
        {
            throw new ValidationAppException("Chuyên mục không tồn tại.");
        }
    }

    private async Task<Dictionary<Guid, string>> LoadCategoryNamesAsync()
    {
        var cats = await categoryRepo.ListAsync();
        return cats.ToDictionary(c => c.Id, c => c.Name);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static PostDto Map(Post p, Dictionary<Guid, string> names) => new(
        p.Id, p.Title, p.Slug, p.Summary, p.Body, p.CategoryId,
        p.CategoryId is { } cid && names.TryGetValue(cid, out var n) ? n : null,
        p.Status, p.PublishedAt, p.LikeCount);
}
