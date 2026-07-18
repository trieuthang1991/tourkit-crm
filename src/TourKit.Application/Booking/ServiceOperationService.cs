using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phiếu điều hành dịch vụ (legacy "Phiếu điều hành dịch vụ") — lớp đọc trên ServiceBooking, theo dõi chi NCC:
/// tổng chi (TotalAmount) · đã thanh toán · còn thiếu. Trạng thái chi: 0 chờ chi (chưa TT),
/// 1 chưa chi hết (TT một phần), 2 thành công (đã TT đủ).
///
/// Số đã chi HIỆU LỰC lấy theo hệ cũ ServiceManager: Σ phiếu chi NCC ĐÃ DUYỆT (PaymentVoucher.IsRecognized)
/// khớp OrderId + ProviderId của booking — không chỉ dựa field denormalized PaidAmount (có thể lệch). Để giữ
/// UI hiện tại không vỡ, số hiệu lực = max(PaidAmount denormalized, Σ phiếu đã ghi nhận): phiếu chỉ làm tăng.
/// Booking không gắn đơn/NCC (không khớp được phiếu) rơi về PaidAmount như cũ.
/// </summary>
public sealed class ServiceOperationService(
    IRepository<ServiceBooking> repo,
    IRepository<Provider> providerRepo,
    IRepository<PaymentVoucher> paymentRepo) : IServiceOperationService
{
    public async Task<PagedResult<ServiceOperationDto>> ListAsync(int page, int size, ServiceOperationListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        var pageItems = all.Skip((page - 1) * size).Take(size).ToList();

        var providerIds = pageItems.Where(s => s.ProviderId is not null).Select(s => s.ProviderId!.Value).ToHashSet();
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);
        var recognized = await BuildRecognizedPaidLookupAsync(pageItems);

        var dtos = pageItems.Select(s => Map(s, providerNames, recognized)).ToList();
        return new PagedResult<ServiceOperationDto>(dtos, all.Count, page, size);
    }

    public async Task<ServiceOperationStatsDto> GetStatsAsync(ServiceOperationListFilter? filter = null)
    {
        var all = await QueryAsync(filter);
        var recognized = await BuildRecognizedPaidLookupAsync(all);

        return new ServiceOperationStatsDto(
            all.Count,
            all.Count(s => PaymentStatusOf(s, EffectivePaid(s, recognized)) == 0),
            all.Count(s => PaymentStatusOf(s, EffectivePaid(s, recognized)) == 1),
            all.Count(s => PaymentStatusOf(s, EffectivePaid(s, recognized)) == 2),
            all.Sum(s => s.TotalAmount),
            all.Sum(s => EffectivePaid(s, recognized)),
            all.Sum(s => s.TotalAmount - EffectivePaid(s, recognized)));
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
        var recognized = await BuildRecognizedPaidLookupAsync([entity]);
        return Map(entity, providerNames, recognized);
    }

    private async Task<List<ServiceBooking>> QueryAsync(ServiceOperationListFilter? filter)
    {
        var f = filter ?? new ServiceOperationListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(s => f.ProviderId == null || s.ProviderId == f.ProviderId);

        // Trạng thái chi lọc theo số hiệu lực (phiếu đã ghi nhận) — cần lookup trước khi lọc theo PaymentStatus.
        var recognized = f.PaymentStatus == null
            ? new Dictionary<(Guid, Guid), decimal>()
            : await BuildRecognizedPaidLookupAsync(all);

        return all
            .Where(s => kw == null || s.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) || s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .Where(s => f.PaymentStatus == null || PaymentStatusOf(s, EffectivePaid(s, recognized)) == f.PaymentStatus)
            .OrderByDescending(s => s.StartDate ?? DateTimeOffset.MinValue)
            .ThenByDescending(s => s.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Σ phiếu chi NCC ĐÃ DUYỆT theo (OrderId, ProviderId). Booking là dòng chi NCC trong đơn; phiếu chi
    /// nối tới đơn+NCC (không có FK trực tiếp tới ServiceBooking) nên khớp ở mức đơn+NCC — hệ cũ cũng gom
    /// công nợ NCC theo đơn. Nhiều booking cùng (đơn, NCC) sẽ chia sẻ cùng số ghi nhận (giới hạn đã biết).
    /// </summary>
    private async Task<Dictionary<(Guid OrderId, Guid ProviderId), decimal>> BuildRecognizedPaidLookupAsync(
        IReadOnlyCollection<ServiceBooking> bookings)
    {
        var keys = bookings
            .Where(b => b.OrderId is not null && b.ProviderId is not null)
            .Select(b => (OrderId: b.OrderId!.Value, ProviderId: b.ProviderId!.Value))
            .ToHashSet();
        if (keys.Count == 0)
        {
            return [];
        }

        var orderIds = keys.Select(k => k.OrderId).ToHashSet();
        var vouchers = await paymentRepo.ListAsync(p => p.IsRecognized && p.ProviderId != null && orderIds.Contains(p.OrderId));

        return vouchers
            .Where(v => keys.Contains((v.OrderId, v.ProviderId!.Value)))
            .GroupBy(v => (v.OrderId, ProviderId: v.ProviderId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Amount));
    }

    /// <summary>Số đã chi hiệu lực = max(denormalized PaidAmount, Σ phiếu đã ghi nhận theo đơn+NCC).</summary>
    private static decimal EffectivePaid(ServiceBooking s, IReadOnlyDictionary<(Guid, Guid), decimal> recognized)
    {
        var rec = s.OrderId is { } oid && s.ProviderId is { } pid
            ? recognized.GetValueOrDefault((oid, pid))
            : 0m;
        return Math.Max(s.PaidAmount, rec);
    }

    private static decimal RecognizedOf(ServiceBooking s, IReadOnlyDictionary<(Guid, Guid), decimal> recognized)
        => s.OrderId is { } oid && s.ProviderId is { } pid ? recognized.GetValueOrDefault((oid, pid)) : 0m;

    /// <summary>0 chờ chi (paid=0) · 1 chưa chi hết (0&lt;paid&lt;total) · 2 thành công (paid>=total, total>0).</summary>
    private static int PaymentStatusOf(ServiceBooking s, decimal paid)
    {
        if (s.TotalAmount > 0 && paid >= s.TotalAmount)
        {
            return 2;
        }

        return paid <= 0 ? 0 : 1;
    }

    private static ServiceOperationDto Map(
        ServiceBooking s,
        IReadOnlyDictionary<Guid, string> providerNames,
        IReadOnlyDictionary<(Guid, Guid), decimal> recognized)
    {
        var effective = EffectivePaid(s, recognized);
        return new ServiceOperationDto(
            s.Id, s.Code,
            s.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
            s.Description, s.StartDate,
            s.TotalAmount, s.PaidAmount, s.TotalAmount - effective, PaymentStatusOf(s, effective),
            RecognizedOf(s, recognized));
    }
}
