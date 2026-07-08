using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Booking;

/// <summary>
/// Test slice Booking/Departure trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>ProviderSlicesTests</c>/<c>CustomerSlicesTests</c>).
/// </summary>
public class BookingSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateDepartureCommand ValidDeparture() => new(
        Guid.NewGuid(), "DEP-1", "Chuyến 1", null, null, 30);

    [Fact]
    public void CreateDepartureValidator_rejects_empty_code()
    {
        var v = new CreateDepartureValidator();

        Assert.False(v.Validate(ValidDeparture() with { Code = "" }).IsValid);
        Assert.False(v.Validate(ValidDeparture() with { Title = "" }).IsValid);
        Assert.True(v.Validate(ValidDeparture()).IsValid);
    }

    [Fact]
    public async Task CreateBookingHandler_returns_NotFound_for_missing_departure()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateBookingHandler(db);
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 0, 0, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task CreateHoldHandler_returns_Validation_when_departure_has_no_template()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var departure = new TourDeparture
        {
            TenantId = tenant.TenantId, Code = "DEP-NOTPL", Title = "Không mẫu", ParentTourId = null, TotalSlots = 10,
        };
        db.TourDepartures.Add(departure);
        await db.SaveChangesAsync();

        var handler = new CreateHoldHandler(db);
        var command = new CreateHoldCommand(departure.Id, Guid.NewGuid(), 1, 0, 0, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task CreateBookingHandler_computes_TotalRevenue_from_seeded_template()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var template = new TourTemplate
        {
            TenantId = tenant.TenantId, Code = "TPL-1", Title = "Đà Nẵng", TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
        };
        db.TourTemplates.Add(template);

        var departure = new TourDeparture
        {
            TenantId = tenant.TenantId, Code = "DEP-1", Title = "Đà Nẵng 20/07", ParentTourId = template.Id, TotalSlots = 30,
        };
        db.TourDepartures.Add(departure);

        var customer = new Customer { TenantId = tenant.TenantId, FullName = "Nguyen Van A" };
        db.Customers.Add(customer);

        await db.SaveChangesAsync();

        var handler = new CreateBookingHandler(db);
        var command = new CreateBookingCommand(departure.Id, customer.Id, AdultQty: 2, ChildQty: 1, ChildSmallQty: 0, BabyQty: 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(13_000_000m, result.Value.TotalRevenue);   // 2*5tr + 1*3tr
    }
}
