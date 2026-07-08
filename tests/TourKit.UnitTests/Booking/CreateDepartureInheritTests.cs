using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Booking;

public sealed class CreateDepartureInheritTests
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
    public async Task Departure_inherits_slots_type_and_itinerary_from_template()
    {
        var db = NewDb(new FixedTenant());
        var tpl = new TourTemplate { Code = "TPL", Title = "Mẫu", TourType = "Nội địa", TotalSlots = 25 };
        db.Add(tpl);
        db.Add(new TourItinerary { TourId = tpl.Id, DayIndex = 1, Title = "Ngày 1" });
        await db.SaveChangesAsync();

        var res = await new CreateDepartureHandler(db).Handle(
            new CreateDepartureCommand(tpl.Id, "DEP", "Chuyến 1", null, null, TotalSlots: 0), default);

        Assert.True(res.IsSuccess);
        var dep = await db.TourDepartures.FirstAsync(d => d.Code == "DEP");
        Assert.Equal(25, dep.TotalSlots);
        Assert.Equal("Nội địa", dep.TourType);
        Assert.True(await db.TourItineraries.AnyAsync(i => i.TourId == dep.Id && i.DayIndex == 1));
    }
}
