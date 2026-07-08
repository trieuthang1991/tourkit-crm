using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>Không duyệt (từ chối) → không ghi nhận.</summary>
public sealed record RejectReceiptCommand(Guid ReceiptId) : ICommand<ReceiptResponse>;

public sealed class RejectReceiptHandler : ICommandHandler<RejectReceiptCommand, ReceiptResponse>
{
    private readonly AppDbContext _db;

    public RejectReceiptHandler(AppDbContext db) => _db = db;

    public async Task<Result<ReceiptResponse>> Handle(RejectReceiptCommand c, CancellationToken ct)
    {
        var receipt = await _db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == c.ReceiptId, ct);
        if (receipt is null)
        {
            return Error.NotFound();
        }

        receipt.Status = 2;          // 2 = từ chối
        receipt.IsRecognized = false;
        await _db.SaveChangesAsync(ct);

        return new ReceiptResponse(
            receipt.Id, receipt.Code, receipt.OrderId, receipt.Amount, receipt.PaymentMethod,
            receipt.IssuedAt, receipt.Partner, receipt.Note, receipt.Status, receipt.IsRecognized);
    }
}
