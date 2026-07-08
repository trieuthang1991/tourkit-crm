using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Booking;

/// <summary>Đặt khách lên chuyến (Order + dòng TourCustomer) + liệt kê đơn. Giá tính từ mẫu tour của chuyến.</summary>
public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/tour-departures/{departureId:guid}/bookings", async (
            Guid departureId, CreateBookingRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var departure = await db.TourDepartures.FirstOrDefaultAsync(d => d.Id == departureId, ct);
            if (departure is null)
            {
                return Results.NotFound();
            }

            if (departure.ParentTourId is null)
            {
                return Invalid("Chuyến chưa gắn mẫu tour để tính giá.");
            }

            var customerExists = await db.Customers.AnyAsync(c => c.Id == body.CustomerId, ct);
            if (!customerExists)
            {
                return Invalid("Khách hàng không tồn tại.");
            }

            var template = await db.TourTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == departure.ParentTourId, ct);
            if (template is null)
            {
                return Invalid("Không tìm thấy mẫu tour của chuyến.");
            }

            var order = new Order
            {
                Code = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                TourDepartureId = departureId,
                CustomerId = body.CustomerId,
                BookingType = 0,
                Status = OrderStatus.Confirmed,
            };

            var totalRevenue = (body.AdultQty * template.PriceAdult)
                + (body.ChildQty * template.PriceChild)
                + (body.ChildSmallQty * template.PriceChildSmall)
                + (body.BabyQty * template.PriceBaby);
            order.TotalRevenue = totalRevenue;

            var line = new TourCustomer
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

            db.Orders.Add(order);
            db.TourCustomers.Add(line);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/orders/{order.Id}", ToResponse(order));
        }).RequireAuthorization(Permissions.BookingCreate);

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

    private static OrderResponse ToResponse(Order o) => new(
        o.Id, o.Code, o.TourDepartureId, o.CustomerId, o.TotalRevenue, o.Status);

    private static BookingLineResponse ToLineResponse(TourCustomer l) => new(
        l.Id, l.Quantity, l.AmountChildren, l.AmountChildrenSmall, l.QuantityBaby,
        l.PriceAdult, l.PriceChild, l.PriceChildSmall, l.PriceBaby,
        l.UpfrontAmount, l.ReservationCode, l.IsMainContact);

    private static IResult Invalid(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
