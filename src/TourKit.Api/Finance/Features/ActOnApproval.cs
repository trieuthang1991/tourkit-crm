using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

/// <summary>
/// Duyệt/từ chối 1 bước của luồng duyệt nhiều cấp — state machine (legacy ReceiptVoucherApproval).
/// Khi đạt Approved ở bước cuối, set voucher IsRecognized=true + Status=1 (cùng hiệu ứng duyệt 1 cấp).
/// Thu hồi (recall) một hành động đã duyệt/từ chối là OUT OF SCOPE — chưa hỗ trợ ở lớp này.
/// </summary>
public sealed record ActOnApprovalCommand(Guid ReceiptId, Guid UserId, bool Approve, string? Note)
    : ICommand<ApprovalResponse>;

public sealed class ActOnApprovalHandler : ICommandHandler<ActOnApprovalCommand, ApprovalResponse>
{
    private readonly AppDbContext _db;

    public ActOnApprovalHandler(AppDbContext db) => _db = db;

    public async Task<Result<ApprovalResponse>> Handle(ActOnApprovalCommand c, CancellationToken ct)
    {
        var approval = await _db.ReceiptApprovals.FirstOrDefaultAsync(a => a.ReceiptVoucherId == c.ReceiptId, ct);
        if (approval is null)
        {
            return Error.NotFound();
        }

        if (approval.Status != ApprovalStatus.InProgress)
        {
            return Error.Conflict("Luồng duyệt đã kết thúc.");
        }

        var stepUsers = await _db.ReceiptApprovalStepUsers
            .Where(su => su.ReceiptApprovalId == approval.Id)
            .ToListAsync(ct);

        var actingStepUser = stepUsers.FirstOrDefault(su =>
            su.StepOrder == approval.CurrentStepOrder &&
            su.UserId == c.UserId &&
            su.Status == StepUserStatus.Pending);
        if (actingStepUser is null)
        {
            return Error.Forbidden();
        }

        actingStepUser.Status = c.Approve ? StepUserStatus.Approved : StepUserStatus.Rejected;
        actingStepUser.ActedAt = DateTimeOffset.UtcNow;
        actingStepUser.Note = c.Note;

        var stepOrders = stepUsers.Select(su => su.StepOrder).Distinct().OrderBy(o => o).ToList();
        var firstStepOrder = stepOrders[0];
        var lastStepOrder = stepOrders[^1];

        if (c.Approve)
        {
            var stepPasses = approval.Method == ApprovalMethod.One ||
                              stepUsers.Where(su => su.StepOrder == approval.CurrentStepOrder)
                                  .All(su => su.Status == StepUserStatus.Approved);

            if (stepPasses)
            {
                if (approval.CurrentStepOrder == lastStepOrder)
                {
                    approval.Status = ApprovalStatus.Approved;
                    var receipt = await _db.ReceiptVouchers.FirstAsync(r => r.Id == c.ReceiptId, ct);
                    receipt.IsRecognized = true;
                    receipt.Status = 1;
                }
                else
                {
                    approval.CurrentStepOrder = stepOrders.First(o => o > approval.CurrentStepOrder);
                }
            }
        }
        else
        {
            if (approval.Method == ApprovalMethod.All)
            {
                approval.Status = ApprovalStatus.Rejected;
            }
            else if (approval.CurrentStepOrder == firstStepOrder)
            {
                approval.Status = ApprovalStatus.Rejected;
            }
            else
            {
                var previousStepOrder = stepOrders.Last(o => o < approval.CurrentStepOrder);
                approval.CurrentStepOrder = previousStepOrder;
                foreach (var su in stepUsers.Where(su => su.StepOrder == previousStepOrder))
                {
                    su.Status = StepUserStatus.Pending;
                    su.ActedAt = null;
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        return await ApprovalResponseBuilder.BuildAsync(_db, approval.Id, ct);
    }
}
