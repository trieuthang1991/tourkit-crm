using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Booking;

public sealed class ServiceBookingServiceTests
{
    private static ServiceBookingService NewService(FakeRepository<ServiceBooking>? repo = null)
        => new(
            repo ?? new FakeRepository<ServiceBooking>(),
            new CreateServiceBookingValidator(),
            new UpdateServiceBookingValidator());

    private static CreateServiceBookingDto Sample(
        ServiceBookingType type = ServiceBookingType.Hotel, Guid? orderId = null, int qty = 2, decimal price = 1_000_000m) =>
        new("SB-1", type, orderId, null, "Khách sạn ABC 4*", null, null, qty, price, 0, null);

    [Fact]
    public async Task CreateAsync_computes_total()
    {
        var service = NewService();

        var booking = await service.CreateAsync(Sample(qty: 3, price: 1_500_000m));

        Assert.Equal(4_500_000m, booking.TotalAmount);
        Assert.Equal(ServiceBookingType.Hotel, booking.Type);
    }

    [Fact]
    public async Task CreateAsync_rejects_empty_description()
    {
        var service = NewService();
        var bad = Sample() with { Description = "" };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task CreateAsync_rejects_end_before_start()
    {
        var service = NewService();
        var start = DateTimeOffset.UtcNow;
        var bad = Sample() with { StartDate = start, EndDate = start.AddDays(-1) };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task ListAsync_filters_by_type_and_order()
    {
        var service = NewService();
        var orderId = Guid.NewGuid();
        await service.CreateAsync(Sample(ServiceBookingType.Hotel, orderId));
        await service.CreateAsync(Sample(ServiceBookingType.Flight, orderId));
        await service.CreateAsync(Sample(ServiceBookingType.Hotel, Guid.NewGuid()));

        var hotels = await service.ListAsync(1, 20, ServiceBookingType.Hotel, null);
        Assert.Equal(2, hotels.Total);

        var byOrderHotel = await service.ListAsync(1, 20, ServiceBookingType.Hotel, orderId);
        Assert.Single(byOrderHotel.Items);
    }

    [Fact]
    public async Task UpdateAsync_missing_throws_NotFound()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateServiceBookingDto(
                "SB-1", ServiceBookingType.Visa, null, null, "Visa", null, null, 1, 1m, 0, null)));
    }
}
