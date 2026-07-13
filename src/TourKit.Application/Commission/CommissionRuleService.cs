using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

public sealed class CommissionRuleService(
    IRepository<CommissionRule> repo,
    IRepository<User> userRepo,
    IValidator<CreateCommissionRuleDto> createValidator,
    IValidator<UpdateCommissionRuleDto> updateValidator) : ICommissionRuleService
{
    public async Task<PagedResult<CommissionRuleDto>> ListAsync(int page, int size, CommissionRuleListFilter? filter = null)
    {
        var f = filter ?? new CommissionRuleListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(r =>
            (f.UserId == null || r.UserId == f.UserId) &&
            (f.Status == null || r.Status == f.Status));

        var userIds = all.Select(r => r.UserId).ToHashSet();
        var userNames = (await userRepo.ListAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName);

        var filtered = all
            .Select(r => Map(r) with { UserName = userNames.GetValueOrDefault(r.UserId) })
            .Where(d => kw == null || (d.UserName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(d => d.Percentage)
            .ToList();

        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<CommissionRuleDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<CommissionRuleStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new CommissionRuleStatsDto(
            all.Count,
            all.Count(r => r.Status == 1),
            all.Count(r => r.Status != 1),
            all.Count == 0 ? 0m : Math.Round(all.Average(r => r.Percentage), 2));
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
