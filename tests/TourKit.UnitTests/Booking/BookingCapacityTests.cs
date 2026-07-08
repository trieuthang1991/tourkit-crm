using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Booking;

public sealed class BookingCapacityTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static async Task<(Guid depId, Guid custId)> SeedAsync(AppDbContext db, int totalSlots, bool closed = false)
    {
        var tpl = new TourTemplate { Code = "T", Title = "T", PriceAdult = 1_000_000 };
        var dep = new TourDeparture { Code = "D", Title = "D", ParentTourId = tpl.Id, TotalSlots = totalSlots, IsClosed = closed };
        var cust = new Customer { FullName = "A" };
        db.AddRange(tpl, dep, cust);
        await db.SaveChangesAsync();
        return (dep.Id, cust.Id);
    }

    [Fact]
    public async Task Booking_over_TotalSlots_is_rejected()
    {
        var db = NewDb(new FixedTenant());
        var (depId, custId) = await SeedAsync(db, totalSlots: 2);
        // Đặt 2 chỗ (đủ sức chứa)
        var first = await BookingFactory.BuildAsync(db, depId, custId, 2, 0, 0, 0, isHold: false, default);
        Assert.True(first.IsSuccess);
        // Đặt thêm 1 chỗ → vượt 2 → Conflict
        var second = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(second.IsFailure);
        Assert.Equal(ErrorType.Conflict, second.Error!.Type);
    }

    [Fact]
    public async Task Cancelled_seats_do_not_count_toward_capacity()
    {
        var db = NewDb(new FixedTenant());
        var (depId, custId) = await SeedAsync(db, totalSlots: 1);
        var first = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(first.IsSuccess);
        // Huỷ chỗ vừa đặt
        var seat = await db.TourCustomers.FirstAsync();
        seat.Status = 1;
        await db.SaveChangesAsync();
        // Đặt lại 1 chỗ → OK vì chỗ cũ đã huỷ
        var again = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Booking_on_closed_departure_is_rejected()
    {
        var db = NewDb(new FixedTenant());
        var (depId, custId) = await SeedAsync(db, totalSlots: 10, closed: true);
        var r = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(r.IsFailure);
        Assert.Equal(ErrorType.Conflict, r.Error!.Type);
    }
}
