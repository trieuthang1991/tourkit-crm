using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Gán (hoặc gỡ, khi SalesUserId = null) nhân viên sales phụ trách đơn.</summary>
public sealed record AssignSalesCommand(Guid OrderId, Guid? SalesUserId) : ICommand<OrderResponse>;

public sealed class AssignSalesHandler : ICommandHandler<AssignSalesCommand, OrderResponse>
{
    private readonly AppDbContext _db;

    public AssignSalesHandler(AppDbContext db) => _db = db;

    public async Task<Result<OrderResponse>> Handle(AssignSalesCommand c, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == c.OrderId, ct);
        if (order is null)
        {
            return Error.NotFound();
        }

        order.SalesUserId = c.SalesUserId;
        await _db.SaveChangesAsync(ct);

        return OrderMapper.ToResponse(order);
    }
}
