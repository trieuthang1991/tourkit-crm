using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Finance.Features;

/// <summary>Build <see cref="ApprovalResponse"/> (approval + step users) — dùng chung giữa Start/Act/Get.</summary>
internal static class ApprovalResponseBuilder
{
    public static async Task<ApprovalResponse> BuildAsync(AppDbContext db, Guid approvalId, CancellationToken ct)
    {
        var approval = await db.ReceiptApprovals.AsNoTracking().FirstAsync(a => a.Id == approvalId, ct);
        var steps = await db.ReceiptApprovalStepUsers.AsNoTracking()
            .Where(su => su.ReceiptApprovalId == approvalId)
            .OrderBy(su => su.StepOrder).ThenBy(su => su.UserId)
            .Select(su => new ApprovalStepUserResponse(su.StepOrder, su.UserId, su.Status, su.ActedAt, su.Note))
            .ToArrayAsync(ct);

        return new ApprovalResponse(approval.Id, approval.ReceiptVoucherId, approval.Method,
            approval.CurrentStepOrder, approval.Status, steps);
    }
}
