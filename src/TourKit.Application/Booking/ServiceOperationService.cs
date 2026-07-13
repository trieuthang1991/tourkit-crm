using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phiếu điều hành dịch vụ (legacy "Phiếu điều hành dịch vụ") — lớp đọc trên ServiceBooking, theo dõi chi NCC:
/// tổng chi (TotalAmount) · đã thanh toán (PaidAmount) · còn thiếu. Trạng thái chi: 0 chờ chi (chưa TT),
/// 1 chưa chi hết (TT một phần), 2 thành công (đã TT đủ).
/// </summary>
public sealed class ServiceOperationService(
    IRepository<ServiceBooking> repo,
    IRepository<Provider> providerRepo) : IServiceOperationService
{
    public async Task<PagedResult<ServiceOperationDto>> ListAsync(int page, int size, ServiceOperationListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        var pageItems = all.Skip((page - 1) * size).Take(size).ToList();

        var providerIds = pageItems.Where(s => s.ProviderId is not null).Select(s => s.ProviderId!.Value).ToHashSet();
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);

        var dtos = pageItems.Select(s => Map(s, providerNames)).ToList();
        return new PagedResult<ServiceOperationDto>(dtos, all.Count, page, size);
    }

    public async Task<ServiceOperationStatsDto> GetStatsAsync(ServiceOperationListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        return new ServiceOperationStatsDto(
            all.Count,
            all.Count(s => PaymentStatusOf(s) == 0),
            all.Count(s => PaymentStatusOf(s) == 1),
            all.Count(s => PaymentStatusOf(s) == 2),
            all.Sum(s => s.TotalAmount),
            all.Sum(s => s.PaidAmount),
            all.Sum(s => s.TotalAmount - s.PaidAmount));
    }

    public async Task<ServiceOperationDto> PayAsync(Guid id, PayServiceOperationDto dto)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (dto.PaidAmount < 0)
        {
            throw new ValidationAppException("Số đã thanh toán phải >= 0.");
        }

        entity.PaidAmount = dto.PaidAmount;
        repo.Update(entity);
        await repo.SaveChangesAsync();

        var providerNames = entity.ProviderId is { } pid
            ? (await providerRepo.ListAsync(p => p.Id == pid)).ToDictionary(p => p.Id, p => p.Name)
            : new Dictionary<Guid, string>();
        return Map(entity, providerNames);
    }

    private async Task<List<ServiceBooking>> QueryAsync(ServiceOperationListFilter? filter)
    {
        var f = filter ?? new ServiceOperationListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(s => f.ProviderId == null || s.ProviderId == f.ProviderId);

        return all
            .Where(s => kw == null || s.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) || s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .Where(s => f.PaymentStatus == null || PaymentStatusOf(s) == f.PaymentStatus)
            .OrderByDescending(s => s.StartDate ?? DateTimeOffset.MinValue)
            .ThenByDescending(s => s.CreatedAt)
            .ToList();
    }

    /// <summary>0 chờ chi (paid=0) · 1 chưa chi hết (0&lt;paid&lt;total) · 2 thành công (paid>=total, total>0).</summary>
    private static int PaymentStatusOf(ServiceBooking s)
    {
        if (s.TotalAmount > 0 && s.PaidAmount >= s.TotalAmount)
        {
            return 2;
        }

        return s.PaidAmount <= 0 ? 0 : 1;
    }

    private static ServiceOperationDto Map(ServiceBooking s, IReadOnlyDictionary<Guid, string> providerNames) => new(
        s.Id, s.Code,
        s.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
        s.Description, s.StartDate,
        s.TotalAmount, s.PaidAmount, s.TotalAmount - s.PaidAmount, PaymentStatusOf(s));
}
