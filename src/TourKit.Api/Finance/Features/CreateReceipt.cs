using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record CreateReceiptCommand(Guid OrderId, decimal Amount, string PaymentMethod, string? Partner, string? Note)
    : ICommand<ReceiptResponse>;

public sealed class CreateReceiptValidator : AbstractValidator<CreateReceiptCommand>
{
    public CreateReceiptValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.");
    }
}

public sealed class CreateReceiptHandler : ICommandHandler<CreateReceiptCommand, ReceiptResponse>
{
    private readonly AppDbContext _db;

    public CreateReceiptHandler(AppDbContext db) => _db = db;

    public async Task<Result<ReceiptResponse>> Handle(CreateReceiptCommand c, CancellationToken ct)
    {
        var orderExists = await _db.Orders.AnyAsync(o => o.Id == c.OrderId, ct);
        if (!orderExists)
        {
            return Error.NotFound();
        }

        var receipt = new ReceiptVoucher
        {
            Code = "RCP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = "Phiếu thu",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = c.OrderId,
            Amount = c.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(c.PaymentMethod) ? "cash" : c.PaymentMethod.Trim(),
            Partner = c.Partner,
            Note = c.Note,
            Status = 0,           // 0 = chờ duyệt
            IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
        };
        _db.ReceiptVouchers.Add(receipt);
        await _db.SaveChangesAsync(ct);

        return new ReceiptResponse(
            receipt.Id, receipt.Code, receipt.OrderId, receipt.Amount, receipt.PaymentMethod,
            receipt.IssuedAt, receipt.Partner, receipt.Note, receipt.Status, receipt.IsRecognized);
    }
}
