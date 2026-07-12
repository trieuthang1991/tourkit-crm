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

    public async Task<PagedResult<PaymentListItemDto>> ListAllAsync(int page, int size)
    {
        var (items, total) = await paymentRepo.PageAsync(page, size);

        // Nạp theo lô: mã đơn + tên NCC (phiếu chi trả cho NCC) để danh sách tổng hiển thị được.
        var orderIds = items.Select(p => p.OrderId).ToHashSet();
        var providerIds = items.Where(p => p.ProviderId != null).Select(p => p.ProviderId!.Value).ToHashSet();
        var orderCodes = (await orderRepo.ListAsync(o => orderIds.Contains(o.Id)))
            .ToDictionary(o => o.Id, o => o.Code);
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id)))
            .ToDictionary(p => p.Id, p => p.Name);

        var rows = items.Select(p => new PaymentListItemDto(
            p.Id, p.Code, p.OrderId, orderCodes.GetValueOrDefault(p.OrderId),
            p.ProviderId, p.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null,
            p.Amount, p.PaymentMethod, p.IssuedAt, p.Partner, p.ReceiverName, p.Status, p.IsRecognized)).ToList();

        return new PagedResult<PaymentListItemDto>(rows, total, page, size);
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
