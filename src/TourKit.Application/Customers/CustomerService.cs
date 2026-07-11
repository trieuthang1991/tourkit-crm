using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Customers.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Customers;

public sealed class CustomerService(
    IRepository<Customer> repo,
    IValidator<CreateCustomerDto> createValidator,
    IValidator<UpdateCustomerDto> updateValidator) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<CustomerDto>(dtos, total, page, size);
    }

    public async Task<CustomerDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            CustomerType = dto.CustomerType,
            Source = dto.Source,
            Tag = dto.Tag,
            TempBalance = dto.TempBalance,
            Email = dto.Email,
            Address = dto.Address,
            DateOfBirth = dto.DateOfBirth,
            IdCardNumber = dto.IdCardNumber,
            PassportNumber = dto.PassportNumber,
            PassportExpiry = dto.PassportExpiry,
            Nationality = dto.Nationality,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        entity.CustomerType = dto.CustomerType;
        entity.Source = dto.Source;
        entity.Tag = dto.Tag;
        entity.TempBalance = dto.TempBalance;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.IdCardNumber = dto.IdCardNumber;
        entity.PassportNumber = dto.PassportNumber;
        entity.PassportExpiry = dto.PassportExpiry;
        entity.Nationality = dto.Nationality;
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

    private static CustomerDto Map(Customer c) =>
        new(c.Id, c.FullName, c.Phone, c.CustomerType, c.Source, c.Tag, c.TempBalance,
            c.Email, c.Address, c.DateOfBirth, c.IdCardNumber, c.PassportNumber, c.PassportExpiry, c.Nationality);
}
