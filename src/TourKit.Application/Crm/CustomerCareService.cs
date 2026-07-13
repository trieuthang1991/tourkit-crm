using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Crm;

/// <summary>Chăm sóc khách hàng (legacy Customer_Care). Create validate Customer tồn tại.</summary>
public sealed class CustomerCareService(
    IRepository<CustomerCare> repo,
    IRepository<Customer> customerRepo,
    IRepository<User> userRepo,
    IValidator<CreateCustomerCareDto> createValidator,
    IValidator<UpdateCustomerCareDto> updateValidator) : ICustomerCareService
{
    public async Task<PagedResult<CustomerCareDto>> ListAsync(int page, int size, CustomerCareListFilter? filter = null)
    {
        var f = filter ?? new CustomerCareListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(c =>
            (f.CustomerId == null || c.CustomerId == f.CustomerId) &&
            (f.AssignedToUserId == null || c.AssignedToUserId == f.AssignedToUserId) &&
            (f.Status == null || c.Status == f.Status));

        var customerNames = (await customerRepo.ListAsync()).ToDictionary(x => x.Id, x => x.FullName);
        var userNames = (await userRepo.ListAsync()).ToDictionary(x => x.Id, x => x.FullName);

        var filtered = all
            .Where(c => kw == null || c.Title.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Status)
            .ThenBy(c => c.RemindAt ?? DateTimeOffset.MaxValue)
            .Select(c => Map(c, customerNames, userNames))
            .ToList();

        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<CustomerCareDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<CustomerCareStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        var now = DateTimeOffset.UtcNow;
        return new CustomerCareStatsDto(
            all.Count,
            all.Count(c => c.Status == 0),
            all.Count(c => c.Status == 1),
            all.Count(c => c.Status == 2),
            all.Count(c => c.RemindAt is { } r && r < now && c.Status != 2));
    }

    public async Task<CustomerCareDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<CustomerCareDto> CreateAsync(CreateCustomerCareDto dto)
    {
        await Validate(createValidator, dto);

        if (!await customerRepo.AnyAsync(c => c.Id == dto.CustomerId))
        {
            throw new ValidationAppException("Khách hàng không tồn tại.");
        }

        var entity = new CustomerCare
        {
            CustomerId = dto.CustomerId,
            Title = dto.Title.Trim(),
            Detail = dto.Detail,
            RemindAt = dto.RemindAt,
            AssignedToUserId = dto.AssignedToUserId,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerCareDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Title = dto.Title.Trim();
        entity.Detail = dto.Detail;
        entity.RemindAt = dto.RemindAt;
        entity.Feedback = dto.Feedback;
        entity.AssignedToUserId = dto.AssignedToUserId;
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

    private static CustomerCareDto Map(CustomerCare c) => new(
        c.Id, c.CustomerId, c.Title, c.Detail, c.RemindAt, c.Feedback, c.AssignedToUserId, c.Status);

    private static CustomerCareDto Map(CustomerCare c, IReadOnlyDictionary<Guid, string> customerNames, IReadOnlyDictionary<Guid, string> userNames) => new(
        c.Id, c.CustomerId, c.Title, c.Detail, c.RemindAt, c.Feedback, c.AssignedToUserId, c.Status,
        customerNames.GetValueOrDefault(c.CustomerId),
        c.AssignedToUserId is { } uid ? userNames.GetValueOrDefault(uid) : null);
}
