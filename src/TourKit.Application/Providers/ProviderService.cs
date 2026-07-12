using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Providers;

public sealed class ProviderService(
    IRepository<Provider> repo,
    IRepository<OrderCost> orderCostRepo,
    IRepository<PaymentVoucher> paymentRepo,
    IValidator<CreateProviderDto> createValidator,
    IValidator<UpdateProviderDto> updateValidator) : IProviderService
{
    public async Task<PagedResult<ProviderDto>> ListAsync(int page, int size, ProviderListFilter? filter = null)
    {
        var f = filter ?? new ProviderListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var (items, total) = await repo.PageAsync(page, size, p =>
            (f.Type == null || (int)p.Type == f.Type) &&
            (f.Status == null || p.Status == f.Status) &&
            (kw == null ||
                p.Code.Contains(kw) ||
                p.Name.Contains(kw) ||
                (p.Phone != null && p.Phone.Contains(kw)) ||
                (p.Email != null && p.Email.Contains(kw)) ||
                (p.ContactPerson != null && p.ContactPerson.Contains(kw))));

        // Làm giàu công nợ NCC cho các dòng trong trang: tổng mua (OrderCost) + đã trả (phiếu chi đã duyệt).
        var ids = items.Select(p => p.Id).ToHashSet();
        var costByProvider = (await orderCostRepo.ListAsync(c => ids.Contains(c.ProviderId)))
            .GroupBy(c => c.ProviderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ActualAmount));
        var paidByProvider = (await paymentRepo.ListAsync(p => p.IsRecognized && p.ProviderId != null && ids.Contains(p.ProviderId.Value)))
            .GroupBy(p => p.ProviderId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var dtos = items
            .Select(p => Map(p, costByProvider.GetValueOrDefault(p.Id), paidByProvider.GetValueOrDefault(p.Id)))
            .ToList();
        return new PagedResult<ProviderDto>(dtos, total, page, size);
    }

    public async Task<ProviderStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new ProviderStatsDto(all.Count, all.Count(p => p.Status == 1), all.Count(p => p.Status != 1));
    }

    public async Task<ProviderDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<ProviderDto> CreateAsync(CreateProviderDto dto)
    {
        await Validate(createValidator, dto);

        var code = dto.Code.Trim();
        if (await repo.AnyAsync(p => p.Code == code))
        {
            throw new ConflictException($"Mã nhà cung cấp '{code}' đã tồn tại.");
        }

        var entity = new Provider
        {
            Code = code,
            Name = dto.Name.Trim(),
            Type = dto.Type,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxCode = dto.TaxCode,
            ContactPerson = dto.ContactPerson,
            BankAccount = dto.BankAccount,
            BankName = dto.BankName,
            PaymentTermId = dto.PaymentTermId,
            Rate = dto.Rate,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateProviderDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Name = dto.Name.Trim();
        entity.Type = dto.Type;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.TaxCode = dto.TaxCode;
        entity.ContactPerson = dto.ContactPerson;
        entity.BankAccount = dto.BankAccount;
        entity.BankName = dto.BankName;
        entity.PaymentTermId = dto.PaymentTermId;
        entity.Rate = dto.Rate;
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

    private static ProviderDto Map(Provider p, decimal totalCost = 0m, decimal paid = 0m) => new(
        p.Id, p.Code, p.Name, p.Type, p.Phone, p.Email, p.Address,
        p.TaxCode, p.ContactPerson, p.BankAccount, p.BankName, p.PaymentTermId, p.Rate, p.Status,
        totalCost, paid, OrderMath.Outstanding(totalCost, paid));
}
