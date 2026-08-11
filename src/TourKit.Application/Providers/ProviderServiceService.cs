using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Entities;
using ProviderServiceEntity = TourKit.Shared.Entities.ProviderService;

namespace TourKit.Application.Providers;

// Đặt bí danh ProviderServiceEntity vì entity TourKit.Shared.Entities.ProviderService trùng tên với
// chính class service này (ProviderServiceService) — tránh compiler ưu tiên nhầm sang class cùng namespace.
public sealed class ProviderServiceService(
    IRepository<ProviderServiceEntity> repo,
    IRepository<Provider> providerRepo,
    IRepository<Currency> currencyRepo,
    IRepository<ServiceItem> serviceItemRepo,
    IValidator<CreateProviderServiceDto> createValidator,
    IValidator<UpdateProviderServiceDto> updateValidator) : IProviderServiceService
{
    public async Task<PagedResult<ProviderServiceDto>> ListAsync(int page, int size, Guid? providerId, string? q = null)
    {
        var rates = await LoadRatesAsync();
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        // Không từ khoá → cắt trang NGAY Ở SQL (theo NCC nếu có), không nạp cả bảng.
        if (kw == null)
        {
            var (items, total) = await repo.PageAsync(
                page, size, providerId is null ? null : x => x.ProviderId == providerId.Value);
            var dtos = items.Select(p => Map(p, RateFor(rates, p.CurrencyCode))).ToList();
            return new PagedResult<ProviderServiceDto>(dtos, total, page, size);
        }

        // Có từ khoá: khớp tên gói giá + tên dịch vụ. Tên dịch vụ nằm ở bảng khác → nạp danh mục (nhỏ)
        // rồi so khớp ở bộ nhớ trên tập ĐÃ thu hẹp theo NCC (StringComparison không dịch được sang SQL).
        var all = await repo.ListAsync(providerId is null ? null : x => x.ProviderId == providerId.Value);
        var itemNames = (await serviceItemRepo.ListAsync()).ToDictionary(s => s.Id, s => s.Name);

        bool MatchQ(ProviderServiceEntity p) =>
            (p.PriceName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.ServiceItemId is Guid sid && itemNames.TryGetValue(sid, out var n)
                && n.Contains(kw, StringComparison.OrdinalIgnoreCase));

        var filtered = all.Where(MatchQ).OrderByDescending(p => p.CreatedAt).ToList();
        var pageDtos = filtered.Skip((page - 1) * size).Take(size)
            .Select(p => Map(p, RateFor(rates, p.CurrencyCode))).ToList();
        return new PagedResult<ProviderServiceDto>(pageDtos, filtered.Count, page, size);
    }

    public async Task<ProviderServiceDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var rates = await LoadRatesAsync();
        return Map(entity, RateFor(rates, entity.CurrencyCode));
    }

    public async Task<ProviderServiceDto> CreateAsync(CreateProviderServiceDto dto)
    {
        await Validate(createValidator, dto);

        if (!await providerRepo.AnyAsync(p => p.Id == dto.ProviderId))
        {
            throw new ValidationAppException($"Nhà cung cấp '{dto.ProviderId}' không tồn tại.");
        }

        var entity = new ProviderServiceEntity
        {
            ProviderId = dto.ProviderId,
            ServiceItemId = dto.ServiceItemId,
            PriceName = dto.PriceName,
            ContractPrice = dto.ContractPrice,
            PublicPrice = dto.PublicPrice,
            CurrencyCode = NormalizeCurrency(dto.CurrencyCode),
            AmountOfPeople = dto.AmountOfPeople,
            Note = dto.Note,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        var rates = await LoadRatesAsync();
        return Map(entity, RateFor(rates, entity.CurrencyCode));
    }

    public async Task UpdateAsync(Guid id, UpdateProviderServiceDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        entity.ServiceItemId = dto.ServiceItemId;
        entity.PriceName = dto.PriceName;
        entity.ContractPrice = dto.ContractPrice;
        entity.PublicPrice = dto.PublicPrice;
        entity.CurrencyCode = NormalizeCurrency(dto.CurrencyCode);
        entity.AmountOfPeople = dto.AmountOfPeople;
        entity.Note = dto.Note;
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

    private static string? NormalizeCurrency(string? code)
    {
        var trimmed = code?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Tỷ giá → VND theo mã tiền tệ: VND/null/không tìm thấy = 1.</summary>
    private async Task<Dictionary<string, decimal>> LoadRatesAsync()
    {
        var currencies = await currencyRepo.ListAsync();
        return currencies.ToDictionary(c => c.Code, c => c.RateToVnd);
    }

    private static decimal RateFor(Dictionary<string, decimal> rates, string? code)
    {
        if (string.IsNullOrEmpty(code) || string.Equals(code, "VND", StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        return rates.GetValueOrDefault(code, 1m);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ProviderServiceDto Map(ProviderServiceEntity p, decimal rate) => new(
        p.Id, p.ProviderId, p.ServiceItemId, p.PriceName, p.ContractPrice, p.PublicPrice,
        p.CurrencyCode, p.ContractPrice * rate, p.PublicPrice * rate,
        p.AmountOfPeople, p.Note, p.Status, ProviderServiceLineProfile.Parse(p.ProfileJson));
}
