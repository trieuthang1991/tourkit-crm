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

        // Từ khoá ở đây chỉ khớp MÃ VÉ — cột của chính bảng này, không phải bảng khác. Nên toàn bộ
        // bộ lọc đẩy được xuống SQL và không cần đường chậm nào: cắt trang ngay ở CSDL.
        //
        // ToLower() thay cho StringComparison.OrdinalIgnoreCase: bản có StringComparison KHÔNG dịch
        // được sang SQL (EF ném lỗi lúc chạy). Phép hạ chữ diễn ra ở Postgres, không ở .NET, nên ba
        // analyzer dưới đây khuyên sai chỗ.
        var kwL = kw?.ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862
        var (pageItems, tong) = await repo.PageAsync(page, size, t => t.CreatedAt, descending: true, t =>
            (f.ProviderId == null || t.ProviderId == f.ProviderId) &&
            (f.OrderId == null || t.OrderId == f.OrderId) &&
            (f.Status == null || t.Status == f.Status) &&
            (f.IsClosed == null || t.IsClosed == f.IsClosed) &&
            (kwL == null || t.TicketCode.ToLower().Contains(kwL)));
#pragma warning restore CA1304, CA1311, CA1862

        var orderIds = pageItems.Select(t => t.OrderId).ToHashSet();
        var providerIds = pageItems.Where(t => t.ProviderId != null).Select(t => t.ProviderId!.Value).ToHashSet();
        var orderCodes = (await orderRepo.ListAsync(o => orderIds.Contains(o.Id))).ToDictionary(o => o.Id, o => o.Code);
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);

        var dtos = pageItems.Select(t => Map(t) with
        {
            OrderCode = orderCodes.GetValueOrDefault(t.OrderId),
            ProviderName = t.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
        }).ToList();
        return new PagedResult<TicketFundDto>(dtos, tong, page, size);
    }

    public async Task<TicketFundStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byClosed = await repo.CountByAsync(t => t.IsClosed);

        return new TicketFundStatsDto(
            byClosed.Values.Sum(), byClosed.GetValueOrDefault(true), byClosed.GetValueOrDefault(false));
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
