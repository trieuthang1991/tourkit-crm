using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Finance;

/// <summary>Quỹ vé ứng (legacy TicketFund) — CRUD phân trang, lọc theo đơn. Cô lập tenant do AppDbContext lo.</summary>
public sealed class TicketFundService(
    IRepository<TicketFund> repo,
    IRepository<Order> orderRepo,
    IRepository<Provider> providerRepo,
    IValidator<CreateTicketFundDto> createValidator) : ITicketFundService
{
    public async Task<PagedResult<TicketFundDto>> ListAsync(int page, int size, TicketFundListFilter? filter = null)
    {
        var f = filter ?? new TicketFundListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(t =>
            (f.ProviderId == null || t.ProviderId == f.ProviderId) &&
            (f.OrderId == null || t.OrderId == f.OrderId) &&
            (f.Status == null || t.Status == f.Status) &&
            (f.IsClosed == null || t.IsClosed == f.IsClosed));

        var filtered = all
            .Where(t => kw == null || t.TicketCode.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();

        var orderIds = pageItems.Select(t => t.OrderId).ToHashSet();
        var providerIds = pageItems.Where(t => t.ProviderId != null).Select(t => t.ProviderId!.Value).ToHashSet();
        var orderCodes = (await orderRepo.ListAsync(o => orderIds.Contains(o.Id))).ToDictionary(o => o.Id, o => o.Code);
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);

        var dtos = pageItems.Select(t => Map(t) with
        {
            OrderCode = orderCodes.GetValueOrDefault(t.OrderId),
            ProviderName = t.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
        }).ToList();
        return new PagedResult<TicketFundDto>(dtos, filtered.Count, page, size);
    }

    public async Task<TicketFundStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new TicketFundStatsDto(all.Count, all.Count(t => t.IsClosed), all.Count(t => !t.IsClosed));
    }

    public async Task<TicketFundDto> CreateAsync(CreateTicketFundDto dto)
    {
        var result = await createValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        var entity = new TicketFund
        {
            OrderId = dto.OrderId,
            ProviderId = dto.ProviderId,
            ProviderServiceId = dto.ProviderServiceId,
            TicketCode = dto.TicketCode?.Trim() ?? string.Empty,
            Status = dto.Status,
            IsClosed = dto.IsClosed,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTicketFundDto dto)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        entity.ProviderId = dto.ProviderId;
        entity.ProviderServiceId = dto.ProviderServiceId;
        entity.TicketCode = dto.TicketCode?.Trim() ?? string.Empty;
        entity.Status = dto.Status;
        entity.IsClosed = dto.IsClosed;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static TicketFundDto Map(TicketFund t) =>
        new(t.Id, t.OrderId, t.ProviderId, t.ProviderServiceId, t.TicketCode, t.Status, t.IsClosed);
}
