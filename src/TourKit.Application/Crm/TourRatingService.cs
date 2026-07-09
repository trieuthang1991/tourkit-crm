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
    public async Task<PagedResult<TourRatingDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<TourRatingDto>(dtos, total, page, size);
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
