using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>Phòng ban (legacy PhongBan) — CRUD list, Name duy nhất/tenant. Gán vào User.DepartmentId.</summary>
public sealed class DepartmentService(
    IRepository<Department> repo,
    IValidator<CreateDepartmentDto> createValidator,
    IValidator<UpdateDepartmentDto> updateValidator) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        await Validate(createValidator, dto);
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, null);

        var entity = new Department { Name = name, Code = dto.Code?.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, id);

        entity.Name = name;
        entity.Code = dto.Code?.Trim();
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
            throw new ValidationAppException($"Phòng ban \"{name}\" đã tồn tại.");
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

    private static DepartmentDto Map(Department m) => new(m.Id, m.Name, m.Code, m.SortOrder, m.Status);
}
