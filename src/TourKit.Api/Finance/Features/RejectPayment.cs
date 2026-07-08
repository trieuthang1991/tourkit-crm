using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>Không duyệt (từ chối) phiếu chi → không ghi nhận dòng tiền.</summary>
public sealed record RejectPaymentCommand(Guid PaymentId) : ICommand<PaymentResponse>;

public sealed class RejectPaymentHandler : ICommandHandler<RejectPaymentCommand, PaymentResponse>
{
    private readonly AppDbContext _db;

    public RejectPaymentHandler(AppDbContext db) => _db = db;

    public async Task<Result<PaymentResponse>> Handle(RejectPaymentCommand c, CancellationToken ct)
    {
        var payment = await _db.PaymentVouchers.FirstOrDefaultAsync(p => p.Id == c.PaymentId, ct);
        if (payment is null)
        {
            return Error.NotFound();
        }

        if (payment.Status != 0)
        {
            return Error.Conflict("Phiếu đã xử lý.");
        }

        payment.Status = 2;          // 2 = từ chối
        payment.IsRecognized = false;
        await _db.SaveChangesAsync(ct);

        return new PaymentResponse(
            payment.Id, payment.Code, payment.OrderId, payment.ProviderId, payment.OrderCostId,
            payment.Amount, payment.PaymentMethod, payment.IssuedAt, payment.Partner, payment.ReceiverName,
            payment.Note, payment.Status, payment.IsRecognized);
    }
}
