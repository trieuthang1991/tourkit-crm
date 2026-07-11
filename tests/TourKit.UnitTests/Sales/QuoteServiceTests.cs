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
        out FakeRepository<QuoteLine> lineRepo)
    {
        quoteRepo = new FakeRepository<Quote>();
        lineRepo = new FakeRepository<QuoteLine>();
        return new QuoteService(quoteRepo, lineRepo, new CreateQuoteValidator(), new UpdateQuoteValidator());
    }

    private static CreateQuoteDto SampleCreate(params (string desc, int qty, decimal price)[] lines) =>
        new(
            "BG-001", null, "Công ty ABC", "Báo giá tour Đà Nẵng 3N2Đ",
            null, 0, null,
            lines.Select(l => new CreateQuoteLineDto(l.desc, l.qty, l.price)).ToArray());

    [Fact]
    public async Task CreateAsync_computes_total_from_lines()
    {
        var service = NewService(out _, out _);

        var quote = await service.CreateAsync(SampleCreate(("Người lớn", 2, 5_000_000m), ("Trẻ em", 1, 3_000_000m)));

        Assert.Equal(13_000_000m, quote.TotalAmount);
        Assert.Equal(2, quote.Lines.Length);
        Assert.Equal(10_000_000m, quote.Lines[0].Amount);
    }

    [Fact]
    public async Task CreateAsync_rejects_empty_code()
    {
        var service = NewService(out _, out _);
        var bad = SampleCreate(("x", 1, 1m)) with { Code = "" };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task UpdateAsync_replaces_lines_and_recomputes_total()
    {
        var service = NewService(out _, out var lineRepo);
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
    public async Task GetAsync_missing_throws_NotFound()
    {
        var service = NewService(out _, out _);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_removes_quote_and_lines()
    {
        var service = NewService(out _, out var lineRepo);
        var created = await service.CreateAsync(SampleCreate(("A", 1, 1m), ("B", 2, 2m)));

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(created.Id));
        Assert.Empty(await lineRepo.ListAsync(l => l.QuoteId == created.Id));
    }
}
