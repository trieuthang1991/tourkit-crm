using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>Chi nhánh (legacy ChiNhanh) — CRUD list, Name duy nhất/tenant. Dùng lọc trên các màn giao dịch.</summary>
public sealed class BranchService(
    IRepository<Branch> repo,
    IValidator<CreateBranchDto> createValidator,
    IValidator<UpdateBranchDto> updateValidator) : IBranchService
{
    public async Task<IReadOnlyList<BranchDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<BranchDto> CreateAsync(CreateBranchDto dto)
    {
        await Validate(createValidator, dto);
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, null);

        var entity = new Branch { Name = name, Code = dto.Code?.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateBranchDto dto)
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
            throw new ValidationAppException($"Chi nhánh \"{name}\" đã tồn tại.");
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

    private static BranchDto Map(Branch b) => new(b.Id, b.Name, b.Code, b.SortOrder, b.Status);
}
