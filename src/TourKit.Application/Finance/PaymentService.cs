using System.Linq.Expressions;
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

        var pm = string.IsNullOrWhiteSpace(f.PaymentMethod) ? null : f.PaymentMethod.Trim();

        // Chi nhánh/sale nằm ở bảng Orders: giải trước ra danh sách OrderId khớp rồi đẩy xuống SQL
        // bằng IN (...), thay vì kéo mọi phiếu chi về RAM chỉ để đối chiếu với đơn.
        var scopeByOrder = f.BranchId != null || f.SalesUserId != null;
        var scopedOrderIds = scopeByOrder
            ? (await orderRepo.ListAsync(o =>
                (f.BranchId == null || o.BranchId == f.BranchId) &&
                (f.SalesUserId == null || o.SalesUserId == f.SalesUserId))).Select(o => o.Id).ToArray()
            : [];

        // Lọc cột thật (trạng thái, ngày, hình thức, số tiền, đơn) ở DB.
        Expression<Func<PaymentVoucher, bool>> predicate = p =>
            (f.Status == null || p.Status == f.Status) &&
            (f.From == null || p.IssuedAt >= f.From) &&
            (f.To == null || p.IssuedAt <= f.To) &&
            (pm == null || p.PaymentMethod.Contains(pm)) &&
            (f.AmountFrom == null || p.Amount >= f.AmountFrom) &&
            (f.AmountTo == null || p.Amount <= f.AmountTo) &&
            (!scopeByOrder || scopedOrderIds.Contains(p.OrderId));

        // Không có từ khoá → cắt trang NGAY Ở SQL rồi mới tra mã đơn/tên NCC cho ĐÚNG mấy dòng
        // của trang. PageAsync đã sắp CreatedAt giảm dần, trùng thứ tự cũ nên kết quả không đổi.
        if (kw == null)
        {
            var (pageEntities, total) = await paymentRepo.PageAsync(page, size, predicate);
            return new PagedResult<PaymentListItemDto>(await ToRowsAsync(pageEntities), total, page, size);
        }

        // Từ khoá đụng cả cột của ĐƠN/NCC và so khớp không phân biệt hoa/thường — không dịch được
        // sang SQL, nên đành làm giàu rồi lọc trong RAM trên tập đã bị predicate trên thu hẹp.
        var all = (await paymentRepo.ListAsync(predicate)).OrderByDescending(p => p.CreatedAt).ToList();
        var rows = await ToRowsAsync(all);

        bool MatchQ(PaymentListItemDto d) =>
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (d.OrderCode?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.ProviderName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.Partner?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.ReceiverName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = rows.Where(MatchQ).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<PaymentListItemDto>(pageItems, filtered.Count, page, size);
    }

    /// <summary>Làm giàu mã đơn + tên NCC CHỈ cho tập phiếu truyền vào (thường là 1 trang).</summary>
    private async Task<List<PaymentListItemDto>> ToRowsAsync(IReadOnlyList<PaymentVoucher> payments)
    {
        var orderIds = payments.Select(p => p.OrderId).ToHashSet();
        var providerIds = payments.Where(p => p.ProviderId != null).Select(p => p.ProviderId!.Value).ToHashSet();
        var orderCodes = orderIds.Count == 0
            ? []
            : (await orderRepo.ListAsync(o => orderIds.Contains(o.Id))).ToDictionary(o => o.Id, o => o.Code);
        var providerNames = providerIds.Count == 0
            ? []
            : (await providerRepo.ListAsync(p => providerIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.Name);

        return payments.Select(p => new PaymentListItemDto(
            p.Id, p.Code, p.OrderId, orderCodes.GetValueOrDefault(p.OrderId),
            p.ProviderId, p.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
            p.Amount, p.PaymentMethod, p.IssuedAt, p.Partner, p.ReceiverName, p.Status, p.IsRecognized)).ToList();
    }

    /// <summary>Đếm và cộng ở SQL — không nạp bảng phiếu chi về bộ nhớ.</summary>
    public async Task<PaymentStatsDto> GetStatsAsync() => new(
        await paymentRepo.CountAsync(),
        await paymentRepo.SumAsync(p => p.Amount),
        await paymentRepo.CountAsync(p => p.Status == 0),
        await paymentRepo.CountAsync(p => p.Status == 1),
        await paymentRepo.CountAsync(p => p.Status == 2));

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
