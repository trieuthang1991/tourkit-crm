using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Booking;

/// <summary>
/// Đặt khách lên chuyến (Order + dòng TourCustomer) + giữ chỗ / xác nhận chỗ / đặt cọc.
/// Trạng thái chỗ suy ra từ upfront_amount vs giá + HoldExpiresAt — theo flow "Giữ chỗ" hệ cũ.
/// </summary>
public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        // Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.
        app.MapPost("/api/v1/tour-departures/{departureId:guid}/bookings", async (
            Guid departureId, CreateBookingRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var (error, order, _) = await BuildBookingAsync(db, departureId, body, isHold: false, ct);
            return error ?? Results.Created($"/api/v1/orders/{order!.Id}", ToResponse(order));
        }).RequireAuthorization(Permissions.BookingCreate);

        // Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).
        app.MapPost("/api/v1/tour-departures/{departureId:guid}/holds", async (
            Guid departureId, CreateBookingRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var (error, _, seat) = await BuildBookingAsync(db, departureId, body, isHold: true, ct);
            return error ?? Results.Created($"/api/v1/tour-customers/{seat!.Id}", ToSeatResponse(seat));
        }).RequireAuthorization(Permissions.BookingCreate);

        // Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".
        app.MapPost("/api/v1/tour-customers/{seatId:guid}/confirm-seat", async (
            Guid seatId, AppDbContext db, CancellationToken ct) =>
        {
            var seat = await db.TourCustomers.FirstOrDefaultAsync(s => s.Id == seatId, ct);
            if (seat is null)
            {
                return Results.NotFound();
            }

            if (seat.UpfrontAmount != 0m)
            {
                return Invalid("Chỉ xác nhận chỗ đang giữ (chưa đặt cọc).");
            }

            seat.HoldExpiresAt = null;   // chốt chỗ, không nhả
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSeatResponse(seat));
        }).RequireAuthorization(Permissions.BookingSeatConfirm);

        // Đặt cọc: cộng vào upfront_amount của chỗ. (Đối soát với phiếu thu = follow-up Finance.)
        app.MapPost("/api/v1/tour-customers/{seatId:guid}/deposit", async (
            Guid seatId, DepositRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (body.Amount <= 0m)
            {
                return Invalid("Số tiền cọc phải lớn hơn 0.");
            }

            var seat = await db.TourCustomers.FirstOrDefaultAsync(s => s.Id == seatId, ct);
            if (seat is null)
            {
                return Results.NotFound();
            }

            seat.UpfrontAmount += body.Amount;
            seat.HoldExpiresAt = null;   // đã có tiền → không còn giữ-chỗ-đếm-ngược
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSeatResponse(seat));
        }).RequireAuthorization(Permissions.BookingCreate);

        app.MapGet("/api/v1/tour-customers/{seatId:guid}", async (
            Guid seatId, AppDbContext db, CancellationToken ct) =>
        {
            var seat = await db.TourCustomers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == seatId, ct);
            return seat is null ? Results.NotFound() : Results.Ok(ToSeatResponse(seat));
        }).RequireAuthorization(Permissions.BookingView);

        app.MapGet("/api/v1/orders", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Orders.AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => ToResponse(o)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.BookingView);

        app.MapGet("/api/v1/orders/{orderId:guid}/lines", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourCustomers.AsNoTracking()
                .Where(l => l.OrderId == orderId)
                .OrderBy(l => l.CreatedAt)
                .Select(l => ToLineResponse(l)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.BookingView);

        return app;
    }

    // Dựng Order + 1 dòng TourCustomer; isHold = true → giữ chỗ (upfront 0 + đếm ngược).
    private static async Task<(IResult? Error, Order? Order, TourCustomer? Seat)> BuildBookingAsync(
        AppDbContext db, Guid departureId, CreateBookingRequest body, bool isHold, CancellationToken ct)
    {
        var departure = await db.TourDepartures.FirstOrDefaultAsync(d => d.Id == departureId, ct);
        if (departure is null)
        {
            return (Results.NotFound(), null, null);
        }

        if (departure.ParentTourId is null)
        {
            return (Invalid("Chuyến chưa gắn mẫu tour để tính giá."), null, null);
        }

        var customerExists = await db.Customers.AnyAsync(c => c.Id == body.CustomerId, ct);
        if (!customerExists)
        {
            return (Invalid("Khách hàng không tồn tại."), null, null);
        }

        var template = await db.TourTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == departure.ParentTourId, ct);
        if (template is null)
        {
            return (Invalid("Không tìm thấy mẫu tour của chuyến."), null, null);
        }

        var lineTotal = (body.AdultQty * template.PriceAdult) + (body.ChildQty * template.PriceChild)
            + (body.ChildSmallQty * template.PriceChildSmall) + (body.BabyQty * template.PriceBaby);

        var order = new Order
        {
            Code = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            TourDepartureId = departureId,
            CustomerId = body.CustomerId,
            BookingType = 0,
            Status = isHold ? OrderStatus.Draft : OrderStatus.Confirmed,
            TotalRevenue = lineTotal,
        };

        var seat = new TourCustomer
        {
            OrderId = order.Id,
            TourDepartureId = departureId,
            CustomerId = body.CustomerId,
            Quantity = body.AdultQty,
            AmountChildren = body.ChildQty,
            AmountChildrenSmall = body.ChildSmallQty,
            QuantityBaby = body.BabyQty,
            PriceAdult = template.PriceAdult,
            PriceChild = template.PriceChild,
            PriceChildSmall = template.PriceChildSmall,
            PriceBaby = template.PriceBaby,
            IsMainContact = true,
        };
        if (isHold)
        {
            seat.HoldExpiresAt = DateTimeOffset.UtcNow.AddHours(template.ReservationHours);
            seat.ReservationCode = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        db.Orders.Add(order);
        db.TourCustomers.Add(seat);
        await db.SaveChangesAsync(ct);
        return (null, order, seat);
    }

    private static OrderResponse ToResponse(Order o) => new(
        o.Id, o.Code, o.TourDepartureId, o.CustomerId, o.TotalRevenue, o.Status);

    private static BookingLineResponse ToLineResponse(TourCustomer l) => new(
        l.Id, l.Quantity, l.AmountChildren, l.AmountChildrenSmall, l.QuantityBaby,
        l.PriceAdult, l.PriceChild, l.PriceChildSmall, l.PriceBaby,
        l.UpfrontAmount, l.ReservationCode, l.IsMainContact);

    private static SeatResponse ToSeatResponse(TourCustomer s)
    {
        var lineTotal = (s.Quantity * s.PriceAdult) + (s.AmountChildren * s.PriceChild)
            + (s.AmountChildrenSmall * s.PriceChildSmall) + (s.QuantityBaby * s.PriceBaby);
        return new SeatResponse(s.Id, s.OrderId, DeriveStatus(s, lineTotal),
            s.UpfrontAmount, lineTotal, s.HoldExpiresAt, s.ReservationCode);
    }

    // Suy trạng thái theo bảng flow "Giữ chỗ" hệ cũ.
    private static SeatStatus DeriveStatus(TourCustomer s, decimal lineTotal)
    {
        if (s.UpfrontAmount >= lineTotal && lineTotal > 0m)
        {
            return SeatStatus.Paid;
        }

        if (s.UpfrontAmount > 0m)
        {
            return SeatStatus.Deposited;
        }

        return s.HoldExpiresAt is not null ? SeatStatus.Held : SeatStatus.HeldConfirmed;
    }

    private static IResult Invalid(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
