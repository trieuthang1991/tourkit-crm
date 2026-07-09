using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Đặt khách "chốt" ngay (không giữ chỗ): Order Confirmed, upfront = 0.</summary>
public sealed record CreateBookingCommand(
    Guid DepartureId, Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty)
    : ICommand<OrderResponse>;

public sealed class CreateBookingHandler : ICommandHandler<CreateBookingCommand, OrderResponse>
{
    private readonly AppDbContext _db;

    public CreateBookingHandler(AppDbContext db) => _db = db;

    public async Task<Result<OrderResponse>> Handle(CreateBookingCommand c, CancellationToken ct)
    {
        var result = await BookingFactory.BuildAsync(
            _db, c.DepartureId, c.CustomerId, c.AdultQty, c.ChildQty, c.ChildSmallQty, c.BabyQty,
            isHold: false, ct);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        return OrderMapper.ToResponse(result.Value.Order);
    }
}

/// <summary>Chiếu Order → OrderResponse — dùng chung giữa các slice booking/order.</summary>
internal static class OrderMapper
{
    public static OrderResponse ToResponse(Order o) => new(
        o.Id, o.Code, o.TourDepartureId, o.CustomerId, o.TotalRevenue, o.TotalCost, o.Status, o.SalesUserId);
}
