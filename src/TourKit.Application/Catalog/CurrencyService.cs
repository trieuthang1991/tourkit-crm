using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>
/// Tỷ giá tiền tệ (legacy ExchangeRate) — CRUD list, Code duy nhất/tenant (chuẩn hoá chữ hoa).
/// Dùng để quy đổi giá vốn NCC ngoại tệ về VND ở ProviderServiceService.
/// </summary>
public sealed class CurrencyService(
    IRepository<Currency> repo,
    IValidator<CreateCurrencyDto> createValidator,
    IValidator<UpdateCurrencyDto> updateValidator) : ICurrencyService
{
    public async Task<IReadOnlyList<CurrencyDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).ThenBy(m => m.Code).Select(Map).ToList();
    }

    public async Task<CurrencyDto> CreateAsync(CreateCurrencyDto dto)
    {
        await Validate(createValidator, dto);
        var code = dto.Code.Trim().ToUpperInvariant();
        await EnsureCodeUnique(code, null);

        var entity = new Currency { Code = code, Name = dto.Name.Trim(), RateToVnd = dto.RateToVnd, SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCurrencyDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var code = dto.Code.Trim().ToUpperInvariant();
        await EnsureCodeUnique(code, id);

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.RateToVnd = dto.RateToVnd;
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

    private async Task EnsureCodeUnique(string code, Guid? excludeId)
    {
        if (await repo.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Mã tiền tệ \"{code}\" đã tồn tại.");
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

    private static CurrencyDto Map(Currency m) => new(m.Id, m.Code, m.Name, m.RateToVnd, m.SortOrder, m.Status);
}
