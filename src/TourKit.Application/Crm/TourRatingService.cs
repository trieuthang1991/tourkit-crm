using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Crm;

/// <summary>Đánh giá sau tour (legacy Rate). Stars 1..5, validate ở Create/Update.</summary>
public sealed class TourRatingService(
    IRepository<TourRating> repo,
    IValidator<CreateTourRatingDto> createValidator,
    IValidator<UpdateTourRatingDto> updateValidator) : ITourRatingService
{
    public async Task<PagedResult<TourRatingDto>> ListAsync(int page, int size, string? q = null)
    {
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        // Không từ khoá (mặc định khi mở màn) → cắt trang NGAY Ở SQL (CreatedAt giảm dần), không nạp cả bảng.
        if (kw == null)
        {
            var (items, total) = await repo.PageAsync(page, size);
            return new PagedResult<TourRatingDto>(items.Select(Map).ToList(), total, page, size);
        }

        // Có từ khoá: so khớp không phân biệt hoa thường trên tên khách/SĐT/nhận xét. EF không dịch được
        // StringComparison nên lọc ở bộ nhớ (LINQ-to-objects, an toàn cho provider InMemory của test).
        var all = await repo.ListAsync();
        bool MatchQ(TourRating r) =>
            (r.CustomerName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.CustomerPhone?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.Comment?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = all.Where(MatchQ).OrderByDescending(r => r.CreatedAt).ToList();
        var dtos = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<TourRatingDto>(dtos, filtered.Count, page, size);
    }

    public async Task<TourRatingDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<TourRatingDto> CreateAsync(CreateTourRatingDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new TourRating
        {
            TourDepartureId = dto.TourDepartureId,
            OrderId = dto.OrderId,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            Stars = dto.Stars,
            Comment = dto.Comment,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTourRatingDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.CustomerName = dto.CustomerName;
        entity.CustomerPhone = dto.CustomerPhone;
        entity.Stars = dto.Stars;
        entity.Comment = dto.Comment;
        entity.Status = dto.Status;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static TourRatingDto Map(TourRating r) => new(
        r.Id, r.TourDepartureId, r.OrderId, r.CustomerName, r.CustomerPhone, r.Stars, r.Comment, r.Status);
}
