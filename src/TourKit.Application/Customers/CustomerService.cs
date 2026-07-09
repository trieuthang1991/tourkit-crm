using FluentValidation;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Customers;

public sealed class CustomerService(
    IRepository<Customer> repo,
    IValidator<CreateCustomerDto> createValidator,
    IValidator<UpdateCustomerDto> updateValidator) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CancellationToken ct = default)
    {
        var (items, total) = await repo.PageAsync(page, size, ct: ct);
        return new PagedResult<CustomerDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default)
        => Map(await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException());

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
    {
        await Validate(createValidator, dto, ct);
        var entity = new Customer { FullName = dto.FullName.Trim(), Phone = dto.Phone };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default)
    {
        await Validate(updateValidator, dto, ct);
        var entity = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException();
        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        repo.Update(entity);
        await repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync(ct);
    }

    private static async Task Validate<T>(IValidator<T> v, T dto, CancellationToken ct)
    {
        var r = await v.ValidateAsync(dto, ct);
        if (!r.IsValid)
        {
            throw new ValidationAppException(r.Errors[0].ErrorMessage);
        }
    }

    private static CustomerDto Map(Customer c) => new(c.Id, c.FullName, c.Phone);
}
