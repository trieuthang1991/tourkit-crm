using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>Khởi tạo luồng duyệt nhiều cấp cho một phiếu thu (tối đa 1 luồng đang tồn tại / phiếu).</summary>
public sealed record StartApprovalCommand(Guid ReceiptId, ApprovalMethod Method, ApprovalStep[] Steps)
    : ICommand<ApprovalResponse>;

public sealed class StartApprovalHandler : ICommandHandler<StartApprovalCommand, ApprovalResponse>
{
    private readonly AppDbContext _db;

    public StartApprovalHandler(AppDbContext db) => _db = db;

    public async Task<Result<ApprovalResponse>> Handle(StartApprovalCommand c, CancellationToken ct)
    {
        var receipt = await _db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == c.ReceiptId, ct);
        if (receipt is null)
        {
            return Error.NotFound();
        }

        var hasExisting = await _db.ReceiptApprovals.AnyAsync(a => a.ReceiptVoucherId == c.ReceiptId, ct);
        if (hasExisting)
        {
            return Error.Conflict("Đã có luồng duyệt cho phiếu thu này.");
        }

        if (c.Steps.Length == 0 || c.Steps.Any(s => s.UserIds.Length == 0))
        {
            return Error.Validation("Phải có ít nhất 1 bước, mỗi bước phải có ít nhất 1 người duyệt.");
        }

        var approval = new ReceiptApproval
        {
            ReceiptVoucherId = c.ReceiptId,
            Method = c.Method,
            CurrentStepOrder = c.Steps.Min(s => s.StepOrder),
            Status = ApprovalStatus.InProgress,
        };
        _db.ReceiptApprovals.Add(approval);

        foreach (var step in c.Steps)
        {
            foreach (var userId in step.UserIds)
            {
                _db.ReceiptApprovalStepUsers.Add(new ReceiptApprovalStepUser
                {
                    ReceiptApprovalId = approval.Id,
                    ReceiptVoucherId = c.ReceiptId,
                    StepOrder = step.StepOrder,
                    UserId = userId,
                    Status = StepUserStatus.Pending,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return await ApprovalResponseBuilder.BuildAsync(_db, approval.Id, ct);
    }
}
