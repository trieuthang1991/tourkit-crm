using TourKit.Application.Common;
using TourKit.Application.Rooms;
using TourKit.Application.Rooms.Dtos;
using TourKit.Application.Rooms.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Rooms;

public sealed class RoomAllotmentServiceTests
{
    private static RoomAllotmentService NewService(FakeRepository<RoomAllotment>? repo = null)
        => new(
            repo ?? new FakeRepository<RoomAllotment>(),
            new FakeRepository<Provider>(),
            new CreateRoomAllotmentValidator(),
            new UpdateRoomAllotmentValidator());

    private static CreateRoomAllotmentDto NewDto(
        string provider = "NCC1", string room = "Deluxe", string? province = null, int dayType = 0,
        int quota = 10, int booked = 3, decimal price = 1_000_000m, DateTimeOffset? date = null)
        => new(provider, room, "Dự án", province, "Nội địa",
            date ?? DateTimeOffset.UtcNow, dayType, quota, booked, price, 4, null);

    [Fact]
    public async Task CreateAsync_rejects_empty_provider()
    {
        var service = NewService();
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(provider: "")));
    }

    [Fact]
    public async Task Create_computes_available()
    {
        var service = NewService();
        var created = await service.CreateAsync(NewDto(quota: 10, booked: 4));
        Assert.Equal(6, created.Available); // 10 - 4
    }

    [Fact]
    public async Task Create_available_never_negative()
    {
        var service = NewService();
        var created = await service.CreateAsync(NewDto(quota: 5, booked: 8));
        Assert.Equal(0, created.Available); // max(0, 5-8)
    }

    [Fact]
    public async Task Stats_counts_daytypes_and_sums_inventory()
    {
        var repo = new FakeRepository<RoomAllotment>();
        var service = NewService(repo);
        await service.CreateAsync(NewDto(provider: "NCC1", dayType: 0, quota: 10, booked: 2));
        await service.CreateAsync(NewDto(provider: "NCC1", dayType: 1, quota: 8, booked: 8));
        await service.CreateAsync(NewDto(provider: "NCC2", dayType: 2, quota: 6, booked: 1));
        await service.CreateAsync(NewDto(provider: "NCC2", dayType: 3, quota: 4, booked: 0));

        var s = await service.GetStatsAsync();
        Assert.Equal(4, s.Cells);
        Assert.Equal(2, s.Providers);          // NCC1, NCC2
        Assert.Equal(28, s.TotalQuota);        // 10+8+6+4
        Assert.Equal(11, s.TotalBooked);       // 2+8+1+0
        Assert.Equal(17, s.TotalAvailable);    // 8+0+5+4
        Assert.Equal(1, s.NormalDays);
        Assert.Equal(1, s.WeekendDays);
        Assert.Equal(1, s.HolidayDays);
        Assert.Equal(1, s.PeakDays);
    }

    [Fact]
    public async Task ListAsync_filters_by_province_and_date_range()
    {
        var service = NewService();
        var d0 = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        await service.CreateAsync(NewDto(province: "Hà Nội", date: d0, room: "HN-room"));
        await service.CreateAsync(NewDto(province: "Đà Nẵng", date: d0.AddDays(20), room: "DN-room"));

        var hn = await service.ListAsync(1, 20, new RoomAllotmentListFilter(Province: "Hà Nội"));
        Assert.Equal("HN-room", Assert.Single(hn.Items).ServiceName);

        var window = await service.ListAsync(1, 20,
            new RoomAllotmentListFilter(DateFrom: d0.AddDays(-1), DateTo: d0.AddDays(1)));
        Assert.Equal("HN-room", Assert.Single(window.Items).ServiceName);
    }
}
