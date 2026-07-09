using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Booking;

/// <summary>
/// Test slice gán sales phụ trách đơn trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>BookingSlicesTests</c>).
/// </summary>
public class AssignSalesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    [Fact]
    public async Task AssignSalesHandler_sets_SalesUserId_on_order()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var order = new Order
        {
            TenantId = tenant.TenantId, Code = "ORD-SALES", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(),
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var salesUserId = Guid.NewGuid();
        var handler = new AssignSalesHandler(db);
        var result = await handler.Handle(new AssignSalesCommand(order.Id, salesUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(salesUserId, result.Value.SalesUserId);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(salesUserId, reloaded.SalesUserId);
    }

    [Fact]
    public async Task AssignSalesHandler_returns_NotFound_for_missing_order()
    {
        var db = NewDb(new FixedTenant());
        var handler = new AssignSalesHandler(db);

        var result = await handler.Handle(new AssignSalesCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
