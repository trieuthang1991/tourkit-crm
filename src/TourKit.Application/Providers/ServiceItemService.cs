using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Providers;

public sealed class ServiceItemService(
    IRepository<ServiceItem> repo,
    IValidator<CreateServiceItemDto> createValidator,
    IValidator<UpdateServiceItemDto> updateValidator) : IServiceItemService
{
    public async Task<PagedResult<ServiceItemDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<ServiceItemDto>(dtos, total, page, size);
    }

    public async Task<ServiceItemDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<ServiceItemDto> CreateAsync(CreateServiceItemDto dto)
    {
        await Validate(createValidator, dto);

        var code = dto.Code.Trim();
        if (await repo.AnyAsync(s => s.Code == code))
        {
            throw new ConflictException($"Mã dịch vụ '{code}' đã tồn tại.");
        }

        var entity = new ServiceItem
        {
            Code = code,
            Name = dto.Name.Trim(),
            Category = dto.Category,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateServiceItemDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Name = dto.Name.Trim();
        entity.Category = dto.Category;
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

    private static ServiceItemDto Map(ServiceItem s) => new(s.Id, s.Code, s.Name, s.Category, s.Status);
}
