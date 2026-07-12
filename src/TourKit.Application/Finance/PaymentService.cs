using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Finance;

/// <summary>Phiếu chi (legacy PaymentVoucher, đối xứng phiếu thu) — chi trả cho NCC theo đơn.</summary>
public sealed class PaymentService(
    IRepository<PaymentVoucher> paymentRepo,
    IRepository<Order> orderRepo,
    IRepository<Provider> providerRepo,
    IValidator<CreatePaymentDto> createValidator) : IPaymentService
{
    public async Task<PaymentDto> CreateAsync(Guid orderId, CreatePaymentDto dto)
    {
        await Validate(createValidator, dto);

        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        if (dto.ProviderId is not null && !await providerRepo.AnyAsync(p => p.Id == dto.ProviderId))
        {
            throw new ValidationAppException("Nhà cung cấp không tồn tại.");
        }

        var payment = new PaymentVoucher
        {
            Code = "PAY-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = "Phiếu chi",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = orderId,
            ProviderId = dto.ProviderId,
            OrderCostId = dto.OrderCostId,
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "cash" : dto.PaymentMethod.Trim(),
            Partner = dto.Partner,
            ReceiverName = dto.ReceiverName,
            Note = dto.Note,
            Status = 0,           // 0 = chờ duyệt
            IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
        };
        await paymentRepo.AddAsync(payment);
        await paymentRepo.SaveChangesAsync();

        return Map(payment);
    }

    public async Task<PaymentDto> ApproveAsync(Guid paymentId)
    {
        var payment = await paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
        {
            throw new NotFoundException();
        }

        if (payment.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        payment.Status = 1;          // 1 = đã duyệt
        payment.IsRecognized = true;
        paymentRepo.Update(payment);
        await paymentRepo.SaveChangesAsync();

        return Map(payment);
    }

    public async Task<PaymentDto> RejectAsync(Guid paymentId)
    {
        var payment = await paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
        {
            throw new NotFoundException();
        }

        if (payment.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        payment.Status = 2;          // 2 = từ chối
        payment.IsRecognized = false;
        paymentRepo.Update(payment);
        await paymentRepo.SaveChangesAsync();

        return Map(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListByOrderAsync(Guid orderId)
    {
        var payments = await paymentRepo.ListAsync(p => p.OrderId == orderId);
        return payments.OrderBy(p => p.IssuedAt).Select(Map).ToList();
    }

    public async Task<PagedResult<PaymentListItemDto>> ListAllAsync(int page, int size, PaymentListFilter? filter = null)
    {
        var f = filter ?? new PaymentListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Lọc cột thật (trạng thái, ngày, hình thức, số tiền) ở DB; q (mã phiếu/mã đơn/NCC/người nhận) sau khi làm giàu.
        var pm = string.IsNullOrWhiteSpace(f.PaymentMethod) ? null : f.PaymentMethod.Trim();
        var all = await paymentRepo.ListAsync(p =>
            (f.Status == null || p.Status == f.Status) &&
            (f.From == null || p.IssuedAt >= f.From) &&
            (f.To == null || p.IssuedAt <= f.To) &&
            (pm == null || p.PaymentMethod.Contains(pm)) &&
            (f.AmountFrom == null || p.Amount >= f.AmountFrom) &&
            (f.AmountTo == null || p.Amount <= f.AmountTo));

        // Nạp theo lô: mã đơn + tên NCC (phiếu chi trả cho NCC) để danh sách tổng hiển thị được.
        var orderIds = all.Select(p => p.OrderId).ToHashSet();
        var providerIds = all.Where(p => p.ProviderId != null).Select(p => p.ProviderId!.Value).ToHashSet();
        var relatedOrders = await orderRepo.ListAsync(o => orderIds.Contains(o.Id));
        var orderCodes = relatedOrders.ToDictionary(o => o.Id, o => o.Code);
        var orderBranch = relatedOrders.ToDictionary(o => o.Id, o => o.BranchId);
        var orderSales = relatedOrders.ToDictionary(o => o.Id, o => o.SalesUserId);
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id)))
            .ToDictionary(p => p.Id, p => p.Name);

        var rows = all.Select(p =>
        {
            var dto = new PaymentListItemDto(
                p.Id, p.Code, p.OrderId, orderCodes.GetValueOrDefault(p.OrderId),
                p.ProviderId, p.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
                p.Amount, p.PaymentMethod, p.IssuedAt, p.Partner, p.ReceiverName, p.Status, p.IsRecognized);
            return (p.CreatedAt, Dto: dto);
        });

        bool MatchQ(PaymentListItemDto d) =>
            kw == null ||
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (d.OrderCode?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.ProviderName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.Partner?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.ReceiverName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = rows
            .Where(x => MatchQ(x.Dto)
                && (f.BranchId == null || orderBranch.GetValueOrDefault(x.Dto.OrderId) == f.BranchId)
                && (f.SalesUserId == null || orderSales.GetValueOrDefault(x.Dto.OrderId) == f.SalesUserId))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(x => x.Dto).ToList();
        return new PagedResult<PaymentListItemDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<PaymentStatsDto> GetStatsAsync()
    {
        var all = await paymentRepo.ListAsync();
        return new PaymentStatsDto(
            all.Count, all.Sum(p => p.Amount),
            all.Count(p => p.Status == 0), all.Count(p => p.Status == 1), all.Count(p => p.Status == 2));
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static PaymentDto Map(PaymentVoucher p) => new(
        p.Id, p.Code, p.OrderId, p.ProviderId, p.OrderCostId, p.Amount, p.PaymentMethod,
        p.IssuedAt, p.Partner, p.ReceiverName, p.Note, p.Status, p.IsRecognized);
}
