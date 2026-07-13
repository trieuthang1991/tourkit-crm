using TourKit.Application.Common;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;
using TourKit.Application.Flights.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Flights;

public sealed class FlightTicketIndividualServiceTests
{
    private static FlightTicketIndividualService NewService(FakeRepository<FlightTicketIndividual>? repo = null)
        => new(
            repo ?? new FakeRepository<FlightTicketIndividual>(),
            new FakeRepository<Provider>(),
            new FakeRepository<Order>(),
            new CreateFlightTicketIndividualValidator(),
            new UpdateFlightTicketIndividualValidator());

    private static CreateFlightTicketIndividualDto NewDto(
        string code = "VMB1", string customer = "Khách", int status = 0,
        decimal sell = 0, decimal received = 0, decimal cost = 0, decimal paid = 0,
        DateTimeOffset? due = null)
        => new(code, null, "PNR", customer, null, null, 0, null, null, null,
            sell, received, cost, paid, due, status, null, null);

    [Fact]
    public async Task CreateAsync_rejects_empty_code()
    {
        var service = NewService();
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(code: "")));
    }

    [Fact]
    public async Task Create_computes_derived_pnl()
    {
        var service = NewService();
        var created = await service.CreateAsync(NewDto("VMB-1", sell: 100, received: 30, cost: 80, paid: 20));

        Assert.Equal(70m, created.ReceivableRemaining); // 100 - 30
        Assert.Equal(60m, created.PayableRemaining);    // 80 - 20
        Assert.Equal(20m, created.Profit);              // 100 - 80
    }

    [Fact]
    public async Task Stats_counts_buckets_and_sums_pnl()
    {
        var repo = new FakeRepository<FlightTicketIndividual>();
        var service = NewService(repo);
        // A: đã duyệt, thu đủ + chi đủ → success
        await service.CreateAsync(NewDto("A", status: 1, sell: 100, received: 100, cost: 80, paid: 80));
        // B: tạo mới, chưa chi + quá hạn + chưa thu hết
        await service.CreateAsync(NewDto("B", status: 0, sell: 50, received: 0, cost: 40, paid: 0, due: DateTimeOffset.UtcNow.AddDays(-1)));
        // C: đã duyệt, chi một phần + chưa thu hết
        await service.CreateAsync(NewDto("C", status: 1, sell: 60, received: 30, cost: 50, paid: 20));

        var s = await service.GetStatsAsync();
        Assert.Equal(3, s.Total);
        Assert.Equal(1, s.New);
        Assert.Equal(2, s.Approved);
        Assert.Equal(1, s.PendingPay);      // B
        Assert.Equal(1, s.PartialPay);      // C
        Assert.Equal(1, s.Success);         // A
        Assert.Equal(2, s.PartialReceive);  // B, C
        Assert.Equal(1, s.Overdue);         // B

        Assert.Equal(210m, s.TotalSell);
        Assert.Equal(130m, s.TotalReceived);
        Assert.Equal(80m, s.TotalReceivable);
        Assert.Equal(170m, s.TotalCost);
        Assert.Equal(100m, s.TotalPaid);
        Assert.Equal(70m, s.TotalPayable);
        Assert.Equal(40m, s.TotalProfit);
    }

    [Fact]
    public async Task ListAsync_filters_by_tab_and_search()
    {
        var service = NewService();
        await service.CreateAsync(NewDto("VMB-OVERDUE", status: 1, sell: 50, cost: 40, paid: 0, due: DateTimeOffset.UtcNow.AddDays(-2)));
        await service.CreateAsync(NewDto("VMB-OK", status: 1, sell: 50, received: 50, cost: 40, paid: 40));

        var overdue = await service.ListAsync(1, 20, new FlightTicketIndividualListFilter(Tab: "overdue"));
        Assert.Equal("VMB-OVERDUE", Assert.Single(overdue.Items).Code);

        var byCode = await service.ListAsync(1, 20, new FlightTicketIndividualListFilter(Q: "VMB-OK"));
        Assert.Equal("VMB-OK", Assert.Single(byCode.Items).Code);
    }
}
