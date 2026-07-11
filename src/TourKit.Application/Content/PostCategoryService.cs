using FluentValidation;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Content;

/// <summary>Danh mục bài viết (legacy CategoriesPost) — CRUD list, Slug duy nhất/tenant.</summary>
public sealed class PostCategoryService(
    IRepository<PostCategory> repo,
    IValidator<CreatePostCategoryDto> createValidator,
    IValidator<UpdatePostCategoryDto> updateValidator) : IPostCategoryService
{
    public async Task<IReadOnlyList<PostCategoryDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<PostCategoryDto> CreateAsync(CreatePostCategoryDto dto)
    {
        await Validate(createValidator, dto);
        var slug = dto.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUnique(slug, null);

        var entity = new PostCategory { Name = dto.Name.Trim(), Slug = slug, SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdatePostCategoryDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var slug = dto.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUnique(slug, id);

        entity.Name = dto.Name.Trim();
        entity.Slug = slug;
        entity.SortOrder = dto.SortOrder;
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

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static PostCategoryDto Map(PostCategory m) => new(m.Id, m.Name, m.Slug, m.SortOrder, m.Status);
}
