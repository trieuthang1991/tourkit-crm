using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

public sealed class MarketTypeService(
    IRepository<MarketType> repo,
    IValidator<CreateMarketTypeDto> createValidator,
    IValidator<UpdateMarketTypeDto> updateValidator) : IMarketTypeService
{
    public async Task<IReadOnlyList<MarketTypeDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<MarketTypeDto> CreateAsync(CreateMarketTypeDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new MarketType { Name = dto.Name.Trim(), ParentId = dto.ParentId, SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateMarketTypeDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Name = dto.Name.Trim();
        entity.ParentId = dto.ParentId;
        entity.SortOrder = dto.SortOrder;
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

    private static MarketTypeDto Map(MarketType m) => new(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status);
}
