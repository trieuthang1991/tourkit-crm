using TourKit.Application.Common;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;
using TourKit.Application.Sales.Validators;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.Sales;

public sealed class QuoteServiceTests
{
    private static QuoteService NewService(
        out FakeRepository<Quote> quoteRepo,
        out FakeRepository<QuoteLine> lineRepo,
        out FakeRepository<ProviderService> priceRepo)
    {
        quoteRepo = new FakeRepository<Quote>();
        lineRepo = new FakeRepository<QuoteLine>();
        priceRepo = new FakeRepository<ProviderService>();
        return new QuoteService(quoteRepo, lineRepo, priceRepo, new CreateQuoteValidator(), new UpdateQuoteValidator());
    }

    private static CreateQuoteDto SampleCreate(params (string desc, int qty, decimal price)[] lines) =>
        new(
            "BG-001", null, "Công ty ABC", "Báo giá tour Đà Nẵng 3N2Đ",
            null, 0, null,
            lines.Select(l => new CreateQuoteLineDto(l.desc, l.qty, l.price)).ToArray());

    [Fact]
    public async Task CreateAsync_computes_total_from_lines()
    {
        var service = NewService(out _, out _, out _);

        var quote = await service.CreateAsync(SampleCreate(("Người lớn", 2, 5_000_000m), ("Trẻ em", 1, 3_000_000m)));

        Assert.Equal(13_000_000m, quote.TotalAmount);
        Assert.Equal(2, quote.Lines.Length);
        Assert.Equal(10_000_000m, quote.Lines[0].Amount);
    }

    [Fact]
    public async Task CreateAsync_rejects_empty_code()
    {
        var service = NewService(out _, out _, out _);
        var bad = SampleCreate(("x", 1, 1m)) with { Code = "" };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task UpdateAsync_replaces_lines_and_recomputes_total()
    {
        var service = NewService(out _, out var lineRepo, out _);
        var created = await service.CreateAsync(SampleCreate(("A", 1, 1_000_000m), ("B", 1, 2_000_000m)));

        var updated = await service.UpdateAsync(created.Id, new UpdateQuoteDto(
            "BG-001", null, "Công ty ABC", "Báo giá (đã sửa)", null, 1, null,
            [new CreateQuoteLineDto("Trọn gói", 4, 5_000_000m)]));

        Assert.Equal(20_000_000m, updated.TotalAmount);
        Assert.Single(updated.Lines);
        // dòng cũ đã bị thay hoàn toàn
        Assert.Single(await lineRepo.ListAsync(l => l.QuoteId == created.Id));
    }

    [Fact]
    public async Task CreateAsync_computes_sell_price_from_cost_and_margin_and_totals()
    {
        var service = NewService(out _, out _, out _);

        // 2 NL, phòng PerPerson 2 đêm vốn 500k LN 20% → bán 600k; xe PerGroup 1 vốn 1tr LN 10% → 1.1tr.
        var quote = await service.CreateAsync(new CreateQuoteDto(
            "BG-002", null, "KH", "Tour dự trù", null, 0, null,
            [
                new CreateQuoteLineDto("Phòng", 2, 0m, ServiceType: 1, Scope: 1, UnitCost: 500_000m, MarginPercent: 20m),
                new CreateQuoteLineDto("Xe", 1, 0m, ServiceType: 2, Scope: 0, UnitCost: 1_000_000m, MarginPercent: 10m),
            ],
            Adults: 2));

        Assert.Equal(600_000m, quote.Lines[0].UnitPrice);                      // vốn×(1+20%)
        Assert.Equal(2 * 500_000m * 2 + 1_000_000m, quote.TotalCost);          // 3tr
        Assert.Equal(2 * 600_000m * 2 + 1_100_000m, quote.TotalAmount);        // 3.5tr
        Assert.Equal(500_000m, quote.TotalProfit);
        Assert.Equal(1_200_000m + 1_100_000m / 2, quote.AdultPrice);           // 1.75tr/NL
    }

    [Fact]
    public async Task CreateAsync_unknown_provider_service_ref_throws()
    {
        var service = NewService(out _, out _, out _);
        var dto = SampleCreate(("x", 1, 1m));
        var bad = dto with
        {
            Lines = [new CreateQuoteLineDto("Phòng", 1, 0m, ProviderServiceId: Guid.NewGuid(), UnitCost: 1m)],
        };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task CreateAsync_valid_provider_service_ref_persists()
    {
        var service = NewService(out _, out _, out var priceRepo);
        var price = new ProviderService { ProviderId = Guid.NewGuid(), PriceName = "Phòng đôi", ContractPrice = 500_000m, Status = 1 };
        await priceRepo.AddAsync(price);
        await priceRepo.SaveChangesAsync();

        var dto = SampleCreate(("x", 1, 1m)) with
        {
            Lines = [new CreateQuoteLineDto("Phòng đôi", 2, 0m, Scope: 1, ProviderServiceId: price.Id, UnitCost: 500_000m, MarginPercent: 20m)],
            Adults = 1,
        };
        var quote = await service.CreateAsync(dto);

        Assert.Equal(price.Id, quote.Lines[0].ProviderServiceId);
        Assert.Equal(1_200_000m, quote.TotalAmount); // 2 đêm × 600k × 1 NL
    }

    [Fact]
    public async Task GetAsync_missing_throws_NotFound()
    {
        var service = NewService(out _, out _, out _);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_removes_quote_and_lines()
    {
        var service = NewService(out _, out var lineRepo, out _);
        var created = await service.CreateAsync(SampleCreate(("A", 1, 1m), ("B", 2, 2m)));

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(created.Id));
        Assert.Empty(await lineRepo.ListAsync(l => l.QuoteId == created.Id));
    }

    [Fact]
    public async Task ListAsync_filters_by_status_converted_and_q_plus_stats()
    {
        var service = NewService(out var quoteRepo, out _, out _);
        await quoteRepo.AddAsync(new Quote { Code = "BG-1", CustomerName = "Nguyễn An", Title = "Hạ Long", Status = 2, TotalAmount = 10m, TotalProfit = 3m, ConvertedOrderId = Guid.NewGuid() });
        await quoteRepo.AddAsync(new Quote { Code = "BG-2", CustomerName = "Trần Bình", Title = "Sapa", Status = 1, TotalAmount = 5m, TotalProfit = 1m });
        await quoteRepo.AddAsync(new Quote { Code = "BG-3", CustomerName = "Lê Cường", Title = "Đà Nẵng", Status = 0, TotalAmount = 7m });
        await quoteRepo.SaveChangesAsync();

        Assert.Equal("BG-1", Assert.Single((await service.ListAsync(1, 20, new QuoteListFilter(Status: 2))).Items).Code);
        Assert.Equal("BG-1", Assert.Single((await service.ListAsync(1, 20, new QuoteListFilter(Converted: true))).Items).Code);
        Assert.Equal(2, (await service.ListAsync(1, 20, new QuoteListFilter(Converted: false))).Total);
        Assert.Equal("BG-2", Assert.Single((await service.ListAsync(1, 20, new QuoteListFilter(Q: "Sapa"))).Items).Code);

        var s = await service.GetStatsAsync();
        Assert.Equal(3, s.Total);
        Assert.Equal(1, s.Accepted);
        Assert.Equal(1, s.Sent);
        Assert.Equal(1, s.Draft);
        Assert.Equal(22m, s.TotalAmount);
        Assert.Equal(4m, s.TotalProfit);
    }
}
