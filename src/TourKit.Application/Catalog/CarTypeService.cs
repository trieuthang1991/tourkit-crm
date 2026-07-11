using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>
/// Danh mục loại xe (legacy CarType) — CRUD list (mirror CustomerTypeService), keyed Code (số ghế)
/// duy nhất/tenant. Code khớp Vehicle.SeatType (int) để hiển thị tên loại xe.
/// </summary>
public sealed class CarTypeService(
    IRepository<CarType> repo,
    IValidator<CreateCarTypeDto> createValidator,
    IValidator<UpdateCarTypeDto> updateValidator) : ICarTypeService
{
    public async Task<IReadOnlyList<CarTypeDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).ThenBy(m => m.Code).Select(Map).ToList();
    }

    public async Task<CarTypeDto> CreateAsync(CreateCarTypeDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureCodeUnique(dto.Code, null);

        var entity = new CarType { Code = dto.Code, Name = dto.Name.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCarTypeDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        await EnsureCodeUnique(dto.Code, id);

        entity.Code = dto.Code;
        entity.Name = dto.Name.Trim();
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

    private async Task EnsureCodeUnique(int code, Guid? excludeId)
    {
        if (await repo.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Loại xe {code} chỗ đã tồn tại.");
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

    private static CarTypeDto Map(CarType m) => new(m.Id, m.Code, m.Name, m.SortOrder, m.Status);
}
