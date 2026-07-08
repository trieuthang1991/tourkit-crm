using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>
/// Dựng Order + 1 dòng TourCustomer — dùng chung giữa <see cref="CreateBookingHandler"/> (chốt ngay)
/// và <see cref="CreateHoldHandler"/> (giữ chỗ). isHold = true → giữ chỗ (upfront 0 + đếm ngược).
/// </summary>
internal static class BookingFactory
{
    public static async Task<Result<(Order Order, TourCustomer Seat)>> BuildAsync(
        AppDbContext db, Guid departureId, Guid customerId,
        int adultQty, int childQty, int childSmallQty, int babyQty, bool isHold, CancellationToken ct)
    {
        var departure = await db.TourDepartures.FirstOrDefaultAsync(d => d.Id == departureId, ct);
        if (departure is null)
        {
            return Error.NotFound();
        }

        if (departure.ParentTourId is null)
        {
            return Error.Validation("Chuyến chưa gắn mẫu tour để tính giá.");
        }

        var customerExists = await db.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!customerExists)
        {
            return Error.Validation("Khách hàng không tồn tại.");
        }

        var template = await db.TourTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == departure.ParentTourId, ct);
        if (template is null)
        {
            return Error.Validation("Không tìm thấy mẫu tour của chuyến.");
        }

        var order = new Order
        {
            Code = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            TourDepartureId = departureId,
            CustomerId = customerId,
            BookingType = 0,
            Status = isHold ? OrderStatus.Draft : OrderStatus.Confirmed,
        };

        var seat = new TourCustomer
        {
            OrderId = order.Id,
            TourDepartureId = departureId,
            CustomerId = customerId,
            Quantity = adultQty,
            AmountChildren = childQty,
            AmountChildrenSmall = childSmallQty,
            QuantityBaby = babyQty,
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

        order.TotalRevenue = BookingMath.LineTotal(seat);   // công thức 1 chỗ

        db.Orders.Add(order);
        db.TourCustomers.Add(seat);
        await db.SaveChangesAsync(ct);
        return (order, seat);
    }
}
