using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>Duyệt phiếu → ghi nhận dòng tiền (mới tính vào công nợ). Mode 1 cấp (Default).</summary>
public sealed record ApproveReceiptCommand(Guid ReceiptId) : ICommand<ReceiptResponse>;

public sealed class ApproveReceiptHandler : ICommandHandler<ApproveReceiptCommand, ReceiptResponse>
{
    private readonly AppDbContext _db;

    public ApproveReceiptHandler(AppDbContext db) => _db = db;

    public async Task<Result<ReceiptResponse>> Handle(ApproveReceiptCommand c, CancellationToken ct)
    {
        var receipt = await _db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == c.ReceiptId, ct);
        if (receipt is null)
        {
            return Error.NotFound();
        }

        receipt.Status = 1;          // 1 = đã duyệt
        receipt.IsRecognized = true;
        await _db.SaveChangesAsync(ct);

        return new ReceiptResponse(
            receipt.Id, receipt.Code, receipt.OrderId, receipt.Amount, receipt.PaymentMethod,
            receipt.IssuedAt, receipt.Partner, receipt.Note, receipt.Status, receipt.IsRecognized);
    }
}
