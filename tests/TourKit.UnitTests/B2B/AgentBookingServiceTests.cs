using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.B2B;

public sealed class AgentBookingServiceTests
{
    private static (AgentBookingService Svc, FakeRepository<AgentQuoteRequest> Quotes) NewService()
    {
        var quotes = new FakeRepository<AgentQuoteRequest>();
        var svc = new AgentBookingService(
            new FakeRepository<AgentBooking>(), quotes, new FakeRepository<AgentPassenger>(), new FakeRepository<Agent>());
        return (svc, quotes);
    }

    private static async Task<Guid> SeedQuoteAsync(FakeRepository<AgentQuoteRequest> quotes, AgentQuoteStatus status, decimal amount = 20_000_000m)
    {
        var id = Guid.NewGuid();
        await quotes.AddAsync(new AgentQuoteRequest
        {
            Id = id,
            AgentId = Guid.NewGuid(),
            ProductName = "Tour",
            PaxCount = 5,
            Status = status,
            QuotedAmount = amount,
        });
        await quotes.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task CreateFromQuote_requires_confirmed_quote()
    {
        var (svc, quotes) = NewService();
        var notConfirmed = await SeedQuoteAsync(quotes, AgentQuoteStatus.Quoted);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateFromQuoteAsync(new CreateAgentBookingDto(notConfirmed, "BK-1", null)));
    }

    [Fact]
    public async Task CreateFromQuote_unknown_quote_throws_Validation()
    {
        var (svc, _) = NewService();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateFromQuoteAsync(new CreateAgentBookingDto(Guid.NewGuid(), "BK-1", null)));
    }

    [Fact]
    public async Task CreateFromQuote_copies_total_and_blocks_duplicate()
    {
        var (svc, quotes) = NewService();
        var quoteId = await SeedQuoteAsync(quotes, AgentQuoteStatus.Confirmed, 30_000_000m);

        var booking = await svc.CreateFromQuoteAsync(new CreateAgentBookingDto(quoteId, "BK-1", null));
        Assert.Equal(30_000_000m, booking.TotalAmount);
        Assert.Equal(quoteId, booking.QuoteRequestId);

        // không tạo trùng từ cùng quote
        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateFromQuoteAsync(new CreateAgentBookingDto(quoteId, "BK-2", null)));
    }

    [Fact]
    public async Task AddAndRemovePassenger_roundtrip()
    {
        var (svc, quotes) = NewService();
        var quoteId = await SeedQuoteAsync(quotes, AgentQuoteStatus.Confirmed);
        var booking = await svc.CreateFromQuoteAsync(new CreateAgentBookingDto(quoteId, "BK-1", null));

        var pax = await svc.AddPassengerAsync(booking.Id,
            new AddAgentPassengerDto("Nguyễn Văn A", null, "P123", "VN", null));
        Assert.Equal("Nguyễn Văn A", pax.FullName);

        var withPax = await svc.GetAsync(booking.Id);
        Assert.Single(withPax.Passengers);

        await svc.RemovePassengerAsync(booking.Id, pax.Id);
        var afterRemove = await svc.GetAsync(booking.Id);
        Assert.Empty(afterRemove.Passengers);
    }

    [Fact]
    public async Task AddPassenger_unknown_booking_throws_NotFound()
    {
        var (svc, _) = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.AddPassengerAsync(Guid.NewGuid(), new AddAgentPassengerDto("X", null, null, null, null)));
    }
}
