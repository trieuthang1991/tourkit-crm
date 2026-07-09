using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

public sealed class CommissionRuleService(
    IRepository<CommissionRule> repo,
    IValidator<CreateCommissionRuleDto> createValidator,
    IValidator<UpdateCommissionRuleDto> updateValidator) : ICommissionRuleService
{
    public async Task<PagedResult<CommissionRuleDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<CommissionRuleDto>(dtos, total, page, size);
    }

    public async Task<CommissionRuleDto> CreateAsync(CreateCommissionRuleDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new CommissionRule { UserId = dto.UserId, Percentage = dto.Percentage, Status = dto.Status };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCommissionRuleDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Percentage = dto.Percentage;
        entity.Status = dto.Status;
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

    private static CommissionRuleDto Map(CommissionRule r) => new(r.Id, r.UserId, r.Percentage, r.Status);
}
