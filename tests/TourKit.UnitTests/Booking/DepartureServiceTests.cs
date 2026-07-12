using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class DepartureServiceTests
{
    private static DepartureService NewService(
        FakeRepository<TourDeparture>? departureRepo = null,
        FakeRepository<TourTemplate>? templateRepo = null,
        FakeRepository<TourItinerary>? itineraryRepo = null)
        => new(
            departureRepo ?? new FakeRepository<TourDeparture>(),
            templateRepo ?? new FakeRepository<TourTemplate>(),
            itineraryRepo ?? new FakeRepository<TourItinerary>(),
            new CreateDepartureValidator());

    [Fact]
    public async Task CreateAsync_rejects_empty_code_or_title()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateAsync(new CreateDepartureDto(null, "", "Tiêu đề", null, null, 10)));
    }

    [Fact]
    public async Task CreateAsync_without_template_keeps_given_values()
    {
        var service = NewService();

        var created = await service.CreateAsync(new CreateDepartureDto(null, "DEP-01", "Chuyến độc lập", null, null, 20));

        Assert.Equal("DEP-01", created.Code);
        Assert.Equal(20, created.TotalSlots);
        Assert.Null(created.TemplateId);
    }

    [Fact]
    public async Task Departure_inherits_slots_type_and_itinerary_from_template()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var templateRepo = new FakeRepository<TourTemplate>();
        var itineraryRepo = new FakeRepository<TourItinerary>();

        var template = new TourTemplate { Code = "TPL-01", Title = "Mẫu Đà Lạt", TourType = "outbound", TotalSlots = 45 };
        await templateRepo.AddAsync(template);
        await templateRepo.SaveChangesAsync();

        await itineraryRepo.AddAsync(new TourItinerary { TourId = template.Id, DayIndex = 2, Title = "Ngày 2" });
        await itineraryRepo.AddAsync(new TourItinerary { TourId = template.Id, DayIndex = 1, Title = "Ngày 1" });
        await itineraryRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, itineraryRepo: itineraryRepo);

        // TotalSlots = 0 → kế thừa từ template (45); có truyền TotalSlots khác 0 thì giữ nguyên giá trị truyền vào.
        var created = await service.CreateAsync(
            new CreateDepartureDto(template.Id, "DEP-TPL", "Chuyến từ mẫu", null, null, 0));

        Assert.Equal(45, created.TotalSlots);
        Assert.Equal(template.Id, created.TemplateId);

        var departureEntity = await departureRepo.GetByIdAsync(created.Id);
        Assert.Equal("outbound", departureEntity!.TourType);

        var days = await itineraryRepo.ListAsync(i => i.TourId == created.Id);
        Assert.Equal(2, days.Count);
        Assert.Contains(days, d => d.DayIndex == 1 && d.Title == "Ngày 1");
        Assert.Contains(days, d => d.DayIndex == 2 && d.Title == "Ngày 2");
    }

    [Fact]
    public async Task Departure_with_explicit_TotalSlots_does_not_override_from_template()
    {
        var templateRepo = new FakeRepository<TourTemplate>();
        var template = new TourTemplate { Code = "TPL-02", Title = "Mẫu", TotalSlots = 45 };
        await templateRepo.AddAsync(template);
        await templateRepo.SaveChangesAsync();

        var service = NewService(templateRepo: templateRepo);

        var created = await service.CreateAsync(
            new CreateDepartureDto(template.Id, "DEP-EXP", "Chuyến", null, null, 10));

        Assert.Equal(10, created.TotalSlots);
    }

    [Fact]
    public async Task CloseAsync_throws_NotFound_for_missing_departure()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.CloseAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseAsync_conflicts_when_already_closed()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var departure = new TourDeparture { Code = "DEP-CLOSE", Title = "Chuyến đóng", TotalSlots = 10 };
        await departureRepo.AddAsync(departure);
        await departureRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo);

        var closed = await service.CloseAsync(departure.Id);
        Assert.Equal(departure.Id, closed.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.CloseAsync(departure.Id));
    }

    [Fact]
    public async Task GetAsync_throws_NotFound_for_missing_departure()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_returns_paged_departures()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var service = NewService(departureRepo: departureRepo);
        await service.CreateAsync(new CreateDepartureDto(null, "DEP-A", "A", null, null, 10));
        await service.CreateAsync(new CreateDepartureDto(null, "DEP-B", "B", null, null, 10));

        var page = await service.ListAsync(1, 20);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task ListAsync_filters_by_tourType_and_q()
    {
        var repo = new FakeRepository<TourDeparture>();
        await repo.AddAsync(new TourDeparture { Code = "DEP-IN", Title = "Inbound HN", TourType = "inbound" });
        await repo.AddAsync(new TourDeparture { Code = "DEP-OUT", Title = "Outbound TL", TourType = "outbound" });
        await repo.SaveChangesAsync();
        var service = NewService(departureRepo: repo);

        Assert.Equal("DEP-IN", Assert.Single((await service.ListAsync(1, 20, new DepartureListFilter(TourType: "inbound"))).Items).Code);
        Assert.Equal("DEP-OUT", Assert.Single((await service.ListAsync(1, 20, new DepartureListFilter(Q: "Outbound"))).Items).Code);
    }

    [Fact]
    public async Task ListAsync_filters_by_isClosed()
    {
        var repo = new FakeRepository<TourDeparture>();
        await repo.AddAsync(new TourDeparture { Code = "OPEN", Title = "Đang mở", IsClosed = false });
        await repo.AddAsync(new TourDeparture { Code = "SHUT", Title = "Đã đóng", IsClosed = true });
        await repo.SaveChangesAsync();
        var service = NewService(departureRepo: repo);

        Assert.Equal("SHUT", Assert.Single((await service.ListAsync(1, 20, new DepartureListFilter(IsClosed: true))).Items).Code);
    }

    [Fact]
    public async Task GetStatsAsync_counts_upcoming_closed_and_slots()
    {
        var repo = new FakeRepository<TourDeparture>();
        var now = DateTimeOffset.UtcNow;
        await repo.AddAsync(new TourDeparture { Code = "U", Title = "Sắp đi", DepartureDate = now.AddDays(5), TotalSlots = 30, IsClosed = false });
        await repo.AddAsync(new TourDeparture { Code = "C", Title = "Đã đóng", DepartureDate = now.AddDays(5), TotalSlots = 20, IsClosed = true });
        await repo.SaveChangesAsync();
        var service = NewService(departureRepo: repo);

        var stats = await service.GetStatsAsync();

        Assert.Equal(2, stats.Total);
        Assert.Equal(1, stats.Upcoming);
        Assert.Equal(1, stats.Closed);
        Assert.Equal(50, stats.TotalSlots);
    }

    [Fact]
    public async Task GetFilterOptionsAsync_returns_distinct_tourTypes()
    {
        var repo = new FakeRepository<TourDeparture>();
        await repo.AddAsync(new TourDeparture { Code = "A", Title = "A", TourType = "inbound" });
        await repo.AddAsync(new TourDeparture { Code = "B", Title = "B", TourType = "outbound" });
        await repo.AddAsync(new TourDeparture { Code = "C", Title = "C", TourType = "inbound" });
        await repo.SaveChangesAsync();
        var service = NewService(departureRepo: repo);

        var opts = await service.GetFilterOptionsAsync();

        Assert.Equal(new[] { "inbound", "outbound" }, opts.TourTypes);
    }
}
