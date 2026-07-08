using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Booking.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking;

/// <summary>
/// Đặt khách lên chuyến (Order + dòng TourCustomer) + giữ chỗ / xác nhận chỗ / đặt cọc.
/// Trạng thái chỗ suy ra từ upfront_amount vs giá + HoldExpiresAt — theo flow "Giữ chỗ" hệ cũ.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        // Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.
        app.MapPost("/api/v1/tour-departures/{departureId:guid}/bookings", async (
            Guid departureId, CreateBookingRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateBookingCommand(
                departureId, body.CustomerId, body.AdultQty, body.ChildQty, body.ChildSmallQty, body.BabyQty);
            var result = await dispatcher.Send(command, ct);
            return result.Match(o => Results.Created($"/api/v1/orders/{o.Id}", o));
        }).RequireAuthorization(Permissions.BookingCreate);

        // Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).
        app.MapPost("/api/v1/tour-departures/{departureId:guid}/holds", async (
            Guid departureId, CreateBookingRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateHoldCommand(
                departureId, body.CustomerId, body.AdultQty, body.ChildQty, body.ChildSmallQty, body.BabyQty);
            var result = await dispatcher.Send(command, ct);
            return result.Match(s => Results.Created($"/api/v1/tour-customers/{s.Id}", s));
        }).RequireAuthorization(Permissions.BookingCreate);

        // Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".
        app.MapPost("/api/v1/tour-customers/{seatId:guid}/confirm-seat", async (
            Guid seatId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ConfirmSeatCommand(seatId), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.BookingSeatConfirm);

        // Đặt cọc: cộng vào upfront_amount của chỗ. (Đối soát với phiếu thu = follow-up Finance.)
        app.MapPost("/api/v1/tour-customers/{seatId:guid}/deposit", async (
            Guid seatId, DepositRequest body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DepositSeatCommand(seatId, body.Amount), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.BookingCreate);

        // Huỷ chỗ + hoàn tiền (legacy CancelSeats + statusCancel != 0).
        app.MapPost("/api/v1/tour-customers/{seatId:guid}/cancel", async (
            Guid seatId, CancelSeatRequest body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new CancelSeatCommand(seatId, body.Note, body.RefundAmount), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.BookingSeatCancel);

        app.MapGet("/api/v1/tour-customers/{seatId:guid}", async (
            Guid seatId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetSeatQuery(seatId), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.BookingView);

        app.MapGet("/api/v1/orders", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListOrdersQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.BookingView);

        app.MapGet("/api/v1/orders/{orderId:guid}/lines", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListOrderLinesQuery(orderId), ct))
                .Match(l => Results.Ok(l))).RequireAuthorization(Permissions.BookingView);

        return app;
    }
}
