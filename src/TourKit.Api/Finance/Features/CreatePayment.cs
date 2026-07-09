using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record CreatePaymentCommand(
    Guid OrderId, Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod,
    string? Partner, string? ReceiverName, string? Note) : ICommand<PaymentResponse>;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.");
    }
}

public sealed class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly AppDbContext _db;

    public CreatePaymentHandler(AppDbContext db) => _db = db;

    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand c, CancellationToken ct)
    {
        var orderExists = await _db.Orders.AnyAsync(o => o.Id == c.OrderId, ct);
        if (!orderExists)
        {
            return Error.NotFound();
        }

        if (c.ProviderId is not null)
        {
            var providerExists = await _db.Providers.AnyAsync(p => p.Id == c.ProviderId, ct);
            if (!providerExists)
            {
                return Error.Validation("Nhà cung cấp không tồn tại.");
            }
        }

        var payment = new PaymentVoucher
        {
            Code = "PAY-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = "Phiếu chi",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = c.OrderId,
            ProviderId = c.ProviderId,
            OrderCostId = c.OrderCostId,
            Amount = c.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(c.PaymentMethod) ? "cash" : c.PaymentMethod.Trim(),
            Partner = c.Partner,
            ReceiverName = c.ReceiverName,
            Note = c.Note,
            Status = 0,           // 0 = chờ duyệt
            IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
        };
        _db.PaymentVouchers.Add(payment);
        await _db.SaveChangesAsync(ct);

        return new PaymentResponse(
            payment.Id, payment.Code, payment.OrderId, payment.ProviderId, payment.OrderCostId,
            payment.Amount, payment.PaymentMethod, payment.IssuedAt, payment.Partner, payment.ReceiverName,
            payment.Note, payment.Status, payment.IsRecognized);
    }
}
