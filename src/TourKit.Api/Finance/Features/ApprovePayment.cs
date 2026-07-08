using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>Duyệt phiếu chi → ghi nhận dòng tiền (mới tính vào công nợ phải trả NCC). Mode 1 cấp (Default).</summary>
public sealed record ApprovePaymentCommand(Guid PaymentId) : ICommand<PaymentResponse>;

public sealed class ApprovePaymentHandler : ICommandHandler<ApprovePaymentCommand, PaymentResponse>
{
    private readonly AppDbContext _db;

    public ApprovePaymentHandler(AppDbContext db) => _db = db;

    public async Task<Result<PaymentResponse>> Handle(ApprovePaymentCommand c, CancellationToken ct)
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

        payment.Status = 1;          // 1 = đã duyệt
        payment.IsRecognized = true;
        await _db.SaveChangesAsync(ct);

        return new PaymentResponse(
            payment.Id, payment.Code, payment.OrderId, payment.ProviderId, payment.OrderCostId,
            payment.Amount, payment.PaymentMethod, payment.IssuedAt, payment.Partner, payment.ReceiverName,
            payment.Note, payment.Status, payment.IsRecognized);
    }
}
