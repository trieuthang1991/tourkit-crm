using Microsoft.EntityFrameworkCore;
using TourKit.Api.Auth;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Finance;

/// <summary>
/// Duyệt phiếu thu NHIỀU CẤP (legacy ReceiptVoucherApproval + ReceiptVoucherApprovalStepUser) —
/// ALTERNATIVE cho duyệt 1 cấp ở ReceiptEndpoints (POST /receipts/{id}/approve vẫn giữ nguyên).
/// Khi luồng nhiều cấp đạt Approved ở bước cuối, set voucher IsRecognized=true + Status=1 (cùng hiệu ứng duyệt 1 cấp).
/// Thu hồi (recall) một hành động đã duyệt/từ chối là OUT OF SCOPE — chưa hỗ trợ ở lớp này.
/// </summary>
public static class ReceiptApprovalEndpoints
{
    public static IEndpointRouteBuilder MapReceiptApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/receipts/{receiptId:guid}/approval", async (
            Guid receiptId, StartApprovalRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var receipt = await db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == receiptId, ct);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            var hasExisting = await db.ReceiptApprovals.AnyAsync(a => a.ReceiptVoucherId == receiptId, ct);
            if (hasExisting)
            {
                return Results.Conflict();
            }

            if (body.Steps.Length == 0 || body.Steps.Any(s => s.UserIds.Length == 0))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Steps"] = ["Phải có ít nhất 1 bước, mỗi bước phải có ít nhất 1 người duyệt."],
                });
            }

            var approval = new ReceiptApproval
            {
                ReceiptVoucherId = receiptId,
                Method = body.Method,
                CurrentStepOrder = body.Steps.Min(s => s.StepOrder),
                Status = ApprovalStatus.InProgress,
            };
            db.ReceiptApprovals.Add(approval);

            foreach (var step in body.Steps)
            {
                foreach (var userId in step.UserIds)
                {
                    db.ReceiptApprovalStepUsers.Add(new ReceiptApprovalStepUser
                    {
                        ReceiptApprovalId = approval.Id,
                        ReceiptVoucherId = receiptId,
                        StepOrder = step.StepOrder,
                        UserId = userId,
                        Status = StepUserStatus.Pending,
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            var response = await BuildResponseAsync(db, approval.Id, ct);
            return Results.Created($"/api/v1/receipts/{receiptId}/approval", response);
        }).RequireAuthorization(Permissions.ReceiptApprovalStart);

        app.MapPost("/api/v1/receipts/{receiptId:guid}/approval/act", async (
            Guid receiptId, ActRequest body, AppDbContext db, ICurrentUser currentUser, CancellationToken ct) =>
        {
            var actingUserId = currentUser.UserId;
            if (actingUserId is null)
            {
                return Results.Unauthorized();
            }

            var approval = await db.ReceiptApprovals.FirstOrDefaultAsync(a => a.ReceiptVoucherId == receiptId, ct);
            if (approval is null)
            {
                return Results.NotFound();
            }

            if (approval.Status != ApprovalStatus.InProgress)
            {
                return Results.Conflict();
            }

            var stepUsers = await db.ReceiptApprovalStepUsers
                .Where(su => su.ReceiptApprovalId == approval.Id)
                .ToListAsync(ct);

            var actingStepUser = stepUsers.FirstOrDefault(su =>
                su.StepOrder == approval.CurrentStepOrder &&
                su.UserId == actingUserId.Value &&
                su.Status == StepUserStatus.Pending);
            if (actingStepUser is null)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            actingStepUser.Status = body.Approve ? StepUserStatus.Approved : StepUserStatus.Rejected;
            actingStepUser.ActedAt = DateTimeOffset.UtcNow;
            actingStepUser.Note = body.Note;

            var stepOrders = stepUsers.Select(su => su.StepOrder).Distinct().OrderBy(o => o).ToList();
            var firstStepOrder = stepOrders[0];
            var lastStepOrder = stepOrders[^1];

            if (body.Approve)
            {
                var stepPasses = approval.Method == ApprovalMethod.One ||
                                  stepUsers.Where(su => su.StepOrder == approval.CurrentStepOrder)
                                      .All(su => su.Status == StepUserStatus.Approved);

                if (stepPasses)
                {
                    if (approval.CurrentStepOrder == lastStepOrder)
                    {
                        approval.Status = ApprovalStatus.Approved;
                        var receipt = await db.ReceiptVouchers.FirstAsync(r => r.Id == receiptId, ct);
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

            await db.SaveChangesAsync(ct);

            var response = await BuildResponseAsync(db, approval.Id, ct);
            return Results.Ok(response);
        }).RequireAuthorization(Permissions.ReceiptApprovalAct);

        app.MapGet("/api/v1/receipts/{receiptId:guid}/approval", async (
            Guid receiptId, AppDbContext db, CancellationToken ct) =>
        {
            var approval = await db.ReceiptApprovals.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ReceiptVoucherId == receiptId, ct);
            if (approval is null)
            {
                return Results.NotFound();
            }

            var response = await BuildResponseAsync(db, approval.Id, ct);
            return Results.Ok(response);
        }).RequireAuthorization(Permissions.ReceiptView);

        return app;
    }

    private static async Task<ApprovalResponse> BuildResponseAsync(AppDbContext db, Guid approvalId, CancellationToken ct)
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
