using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance;

/// <summary>
/// Phiếu chi (đối xứng phiếu thu) — chi trả cho NCC theo đơn. Endpoint mỏng: map request →
/// command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/payments", async (
            Guid orderId, CreatePaymentRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var cmd = new CreatePaymentCommand(orderId, body.ProviderId, body.OrderCostId, body.Amount,
                body.PaymentMethod, body.Partner, body.ReceiverName, body.Note);
            var result = await dispatcher.Send(cmd, ct);
            return result.Match(p => Results.Created($"/api/v1/orders/{orderId}/payments/{p.Id}", p));
        }).RequireAuthorization(Permissions.PaymentCreate);

        app.MapPost("/api/v1/payments/{paymentId:guid}/approve", async (
            Guid paymentId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ApprovePaymentCommand(paymentId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.PaymentApprove);

        app.MapPost("/api/v1/payments/{paymentId:guid}/reject", async (
            Guid paymentId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new RejectPaymentCommand(paymentId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.PaymentApprove);

        app.MapGet("/api/v1/orders/{orderId:guid}/payments", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListPaymentsQuery(orderId), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.PaymentView);

        return app;
    }
}
