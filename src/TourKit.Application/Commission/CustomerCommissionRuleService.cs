using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

/// <summary>Hoa hồng theo loại khách — CRUD phân trang (mirror CommissionRuleService, keyed CustomerType).</summary>
public sealed class CustomerCommissionRuleService(
    IRepository<CustomerCommissionRule> repo,
    IRepository<CustomerType> customerTypeRepo,
    IValidator<CreateCustomerCommissionRuleDto> createValidator,
    IValidator<UpdateCustomerCommissionRuleDto> updateValidator) : ICustomerCommissionRuleService
{
    public async Task<PagedResult<CustomerCommissionRuleDto>> ListAsync(int page, int size, CustomerCommissionRuleListFilter? filter = null)
    {
        var f = filter ?? new CustomerCommissionRuleListFilter();

        var all = await repo.ListAsync(r =>
            (f.CustomerType == null || r.CustomerType == f.CustomerType) &&
            (f.Status == null || r.Status == f.Status));

        var names = (await customerTypeRepo.ListAsync()).ToDictionary(t => t.Code, t => t.Name);

        var ordered = all
            .Select(r => Map(r) with { CustomerTypeName = names.GetValueOrDefault(r.CustomerType) })
            .OrderByDescending(d => d.Percentage)
            .ToList();

        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<CustomerCommissionRuleDto>(pageItems, ordered.Count, page, size);
    }

    public async Task<CustomerCommissionRuleStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new CustomerCommissionRuleStatsDto(
            all.Count,
            all.Count(r => r.Status == 1),
            all.Count(r => r.Status != 1),
            all.Count == 0 ? 0m : Math.Round(all.Average(r => r.Percentage), 2));
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
