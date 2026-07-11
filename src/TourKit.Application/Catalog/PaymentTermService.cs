using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>Điều khoản thanh toán NCC (legacy ServicePaymentTerm) — CRUD list, Name duy nhất/tenant.</summary>
public sealed class PaymentTermService(
    IRepository<PaymentTerm> repo,
    IValidator<CreatePaymentTermDto> createValidator,
    IValidator<UpdatePaymentTermDto> updateValidator) : IPaymentTermService
{
    public async Task<IReadOnlyList<PaymentTermDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).Select(Map).ToList();
    }

    public async Task<PaymentTermDto> CreateAsync(CreatePaymentTermDto dto)
    {
        await Validate(createValidator, dto);
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, null);

        var entity = new PaymentTerm { Name = name, Description = dto.Description?.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdatePaymentTermDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, id);

        entity.Name = name;
        entity.Description = dto.Description?.Trim();
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
            throw new ValidationAppException($"Điều khoản \"{name}\" đã tồn tại.");
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

    private static PaymentTermDto Map(PaymentTerm m) => new(m.Id, m.Name, m.Description, m.SortOrder, m.Status);
}
