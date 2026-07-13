using TourKit.Application.Common;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;
using TourKit.Application.Flights.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Flights;

public sealed class FlightTicketServiceTests
{
    private static FlightTicketService NewService(FakeRepository<FlightTicket>? repo = null)
        => new(
            repo ?? new FakeRepository<FlightTicket>(),
            new FakeRepository<MarketType>(),
            new FakeRepository<Provider>(),
            new FakeRepository<Order>(),
            new FakeRepository<TourDeparture>(),
            new CreateFlightTicketValidator());

    private static CreateFlightTicketDto NewDto(string pnr = "PNR1", int qty = 10, decimal cost = 100m)
        => new(pnr, null, null, "outbound", 5, null, qty, cost, 0m, null, null);

    [Fact]
    public async Task CreateAsync_rejects_empty_pnr()
    {
        var service = NewService();
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(pnr: "")));
    }

    [Fact]
    public async Task Create_then_stats_then_assign_then_delete_roundtrip()
    {
        var repo = new FakeRepository<FlightTicket>();
        var service = NewService(repo);

        var created = await service.CreateAsync(NewDto("PNR-001", 30, 180m));
        Assert.Equal("PNR-001", created.Pnr);
        Assert.Equal(30, created.RemainingQuantity); // chưa dùng vé nào

        var stats = await service.GetStatsAsync();
        Assert.Equal(1, stats.Total);
        Assert.Equal(1, stats.Unassigned);
        Assert.Equal(30, stats.TotalQuantity);
        Assert.Equal(180m, stats.TotalCost);

        await service.AssignAsync(created.Id, new AssignFlightTicketDto("ORDER-XYZ"));
        var assigned = await service.ListAsync(1, 20, new FlightTicketListFilter(Assigned: true));
        Assert.Single(assigned.Items);
        Assert.Empty((await service.ListAsync(1, 20, new FlightTicketListFilter(Assigned: false))).Items);

        await service.DeleteAsync(created.Id);
        Assert.Empty((await service.ListAsync(1, 20)).Items);
    }

    [Fact]
    public async Task UpdateAsync_rejects_used_over_quantity()
    {
        var repo = new FakeRepository<FlightTicket>();
        var service = NewService(repo);
        var created = await service.CreateAsync(NewDto("PNR-002", 10, 50m));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.UpdateAsync(created.Id, new UpdateFlightTicketDto("PNR-002", null, null, "outbound", 5, null, 10, 15, null, 50m, 0m, 0m, 1, null, null)));
    }

    [Fact]
    public async Task ListAsync_filters_by_pnr_and_tourtype()
    {
        var service = NewService();
        await service.CreateAsync(NewDto("SGN-001", 10, 100m) with { TourType = "outbound" });
        await service.CreateAsync(NewDto("HAN-002", 5, 50m) with { TourType = "inbound" });

        Assert.Equal("SGN-001", Assert.Single((await service.ListAsync(1, 20, new FlightTicketListFilter(Q: "SGN"))).Items).Pnr);
        Assert.Equal("HAN-002", Assert.Single((await service.ListAsync(1, 20, new FlightTicketListFilter(TourType: "inbound"))).Items).Pnr);
    }
}
