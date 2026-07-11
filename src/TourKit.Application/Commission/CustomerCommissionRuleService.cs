using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

/// <summary>Hoa hồng theo loại khách — CRUD phân trang (mirror CommissionRuleService, keyed CustomerType).</summary>
public sealed class CustomerCommissionRuleService(
    IRepository<CustomerCommissionRule> repo,
    IValidator<CreateCustomerCommissionRuleDto> createValidator,
    IValidator<UpdateCustomerCommissionRuleDto> updateValidator) : ICustomerCommissionRuleService
{
    public async Task<PagedResult<CustomerCommissionRuleDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        return new PagedResult<CustomerCommissionRuleDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<CustomerCommissionRuleDto> CreateAsync(CreateCustomerCommissionRuleDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new CustomerCommissionRule
        {
            CustomerType = dto.CustomerType,
            Percentage = dto.Percentage,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerCommissionRuleDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.Percentage = dto.Percentage;
        entity.Status = dto.Status;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
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

    private static CustomerCommissionRuleDto Map(CustomerCommissionRule r) =>
        new(r.Id, r.CustomerType, r.Percentage, r.Status);
}
