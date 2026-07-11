using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>
/// Danh mục loại khách (legacy customer_type) — CRUD list (mirror MarketTypeService), keyed Code duy nhất/tenant.
/// Code khớp Customer.CustomerType (int) để hiển thị tên + phục vụ hoa hồng theo loại khách.
/// </summary>
public sealed class CustomerTypeService(
    IRepository<CustomerType> repo,
    IValidator<CreateCustomerTypeDto> createValidator,
    IValidator<UpdateCustomerTypeDto> updateValidator) : ICustomerTypeService
{
    public async Task<IReadOnlyList<CustomerTypeDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<CustomerTypeDto> CreateAsync(CreateCustomerTypeDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureCodeUnique(dto.Code, null);

        var entity = new CustomerType { Code = dto.Code, Name = dto.Name.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerTypeDto dto)
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
            throw new ValidationAppException($"Mã loại khách (Code={code}) đã tồn tại.");
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

    private static CustomerTypeDto Map(CustomerType m) => new(m.Id, m.Code, m.Name, m.SortOrder, m.Status);
}
