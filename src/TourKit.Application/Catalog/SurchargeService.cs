using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>Danh mục loại phụ thu (legacy ConfigSurcharge) — CRUD list, Name duy nhất/tenant.</summary>
public sealed class SurchargeService(
    IRepository<Surcharge> repo,
    IValidator<CreateSurchargeDto> createValidator,
    IValidator<UpdateSurchargeDto> updateValidator) : ISurchargeService
{
    public async Task<IReadOnlyList<SurchargeDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<SurchargeDto> CreateAsync(CreateSurchargeDto dto)
    {
        await Validate(createValidator, dto);
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, null);

        var entity = new Surcharge { Name = name, CalcType = dto.CalcType, DefaultValue = dto.DefaultValue, SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateSurchargeDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, id);

        entity.Name = name;
        entity.CalcType = dto.CalcType;
        entity.DefaultValue = dto.DefaultValue;
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

    private async Task EnsureNameUnique(string name, Guid? excludeId)
    {
        if (await repo.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Loại phụ thu \"{name}\" đã tồn tại.");
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

    private static SurchargeDto Map(Surcharge m) => new(m.Id, m.Name, m.CalcType, m.DefaultValue, m.SortOrder, m.Status);
}
