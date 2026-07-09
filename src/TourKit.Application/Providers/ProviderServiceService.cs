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
    IValidator<CreateProviderServiceDto> createValidator,
    IValidator<UpdateProviderServiceDto> updateValidator) : IProviderServiceService
{
    public async Task<PagedResult<ProviderServiceDto>> ListAsync(int page, int size, Guid? providerId)
    {
        var (items, total) = await repo.PageAsync(
            page, size, providerId is null ? null : x => x.ProviderId == providerId.Value);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<ProviderServiceDto>(dtos, total, page, size);
    }

    public async Task<ProviderServiceDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
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
            AmountOfPeople = dto.AmountOfPeople,
            Note = dto.Note,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateProviderServiceDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.ServiceItemId = dto.ServiceItemId;
        entity.PriceName = dto.PriceName;
        entity.ContractPrice = dto.ContractPrice;
        entity.PublicPrice = dto.PublicPrice;
        entity.AmountOfPeople = dto.AmountOfPeople;
        entity.Note = dto.Note;
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

    private static ProviderServiceDto Map(ProviderServiceEntity p) => new(
        p.Id, p.ProviderId, p.ServiceItemId, p.PriceName, p.ContractPrice, p.PublicPrice,
        p.AmountOfPeople, p.Note, p.Status);
}
