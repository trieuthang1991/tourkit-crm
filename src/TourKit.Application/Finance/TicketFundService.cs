using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Finance;

/// <summary>Quỹ vé ứng (legacy TicketFund) — CRUD phân trang, lọc theo đơn. Cô lập tenant do AppDbContext lo.</summary>
public sealed class TicketFundService(
    IRepository<TicketFund> repo,
    IValidator<CreateTicketFundDto> createValidator) : ITicketFundService
{
    public async Task<PagedResult<TicketFundDto>> ListAsync(int page, int size, Guid? orderId)
    {
        var (items, total) = orderId is { } oid
            ? await repo.PageAsync(page, size, t => t.OrderId == oid)
            : await repo.PageAsync(page, size);
        return new PagedResult<TicketFundDto>(items.Select(Map).ToList(), total, page, size);
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
