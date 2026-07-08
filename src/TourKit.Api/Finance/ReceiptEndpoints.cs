using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance;

/// <summary>
/// Phiếu thu + công nợ theo đơn. Công nợ = Order.TotalRevenue − tổng phiếu thu (tính động).
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/receipts", async (
            Guid orderId, CreateReceiptRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateReceiptCommand(orderId, body.Amount, body.PaymentMethod, body.Partner, body.Note);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/orders/{orderId}/receipts/{r.Id}", r));
        }).RequireAuthorization(Permissions.ReceiptCreate);

        // Duyệt phiếu → ghi nhận dòng tiền (mới tính vào công nợ). Mode 1 cấp (Default).
        app.MapPost("/api/v1/receipts/{receiptId:guid}/approve", async (
            Guid receiptId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ApproveReceiptCommand(receiptId), ct))
                .Match(r => Results.Ok(r))).RequireAuthorization(Permissions.ReceiptApprove);

        // Không duyệt (từ chối) → không ghi nhận.
        app.MapPost("/api/v1/receipts/{receiptId:guid}/reject", async (
            Guid receiptId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new RejectReceiptCommand(receiptId), ct))
                .Match(r => Results.Ok(r))).RequireAuthorization(Permissions.ReceiptApprove);

        app.MapGet("/api/v1/orders/{orderId:guid}/receipts", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListReceiptsQuery(orderId), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.ReceiptView);

        app.MapGet("/api/v1/orders/{orderId:guid}/balance", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetOrderBalanceQuery(orderId), ct))
                .Match(b => Results.Ok(b))).RequireAuthorization(Permissions.ReceiptView);

        return app;
    }
}
