using TourKit.Api.Application;
using TourKit.Api.Auth;
using TourKit.Api.Authz;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance;

/// <summary>
/// Duyệt phiếu thu NHIỀU CẤP (legacy ReceiptVoucherApproval + ReceiptVoucherApprovalStepUser) —
/// ALTERNATIVE cho duyệt 1 cấp ở ReceiptEndpoints (POST /receipts/{id}/approve vẫn giữ nguyên).
/// Khi luồng nhiều cấp đạt Approved ở bước cuối, set voucher IsRecognized=true + Status=1 (cùng hiệu ứng duyệt 1 cấp).
/// Thu hồi (recall) một hành động đã duyệt/từ chối là OUT OF SCOPE — chưa hỗ trợ ở lớp này.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class ReceiptApprovalEndpoints
{
    public static IEndpointRouteBuilder MapReceiptApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/receipts/{receiptId:guid}/approval", async (
            Guid receiptId, StartApprovalRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new StartApprovalCommand(receiptId, body.Method, body.Steps);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/receipts/{receiptId}/approval", r));
        }).RequireAuthorization(Permissions.ReceiptApprovalStart);

        // Acting user lấy từ ICurrentUser ở endpoint (không phải handler) để giữ nguyên 401 khi thiếu current user
        // (handler chỉ nhận UserId đã xác định, trả Error.Forbidden(→403) cho case không phải người duyệt hợp lệ).
        app.MapPost("/api/v1/receipts/{receiptId:guid}/approval/act", async (
            Guid receiptId, ActRequest body, ICurrentUser currentUser, IDispatcher dispatcher, CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
            {
                return Results.Unauthorized();
            }

            var command = new ActOnApprovalCommand(receiptId, currentUser.UserId.Value, body.Approve, body.Note);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Ok(r));
        }).RequireAuthorization(Permissions.ReceiptApprovalAct);

        app.MapGet("/api/v1/receipts/{receiptId:guid}/approval", async (
            Guid receiptId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetApprovalQuery(receiptId), ct))
                .Match(r => Results.Ok(r))).RequireAuthorization(Permissions.ReceiptView);

        return app;
    }
}
