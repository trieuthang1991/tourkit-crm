using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Booking;

public sealed class BookingServiceTests
{
    private static BookingService NewService(
        FakeRepository<TourDeparture>? departureRepo = null,
        FakeRepository<TourCustomer>? seatRepo = null,
        FakeRepository<Order>? orderRepo = null,
        FakeRepository<Customer>? customerRepo = null,
        FakeRepository<TourTemplate>? templateRepo = null,
        FakeRepository<CancelSeat>? cancelSeatRepo = null)
        => new(
            departureRepo ?? new FakeRepository<TourDeparture>(),
            seatRepo ?? new FakeRepository<TourCustomer>(),
            orderRepo ?? new FakeRepository<Order>(),
            customerRepo ?? new FakeRepository<Customer>(),
            templateRepo ?? new FakeRepository<TourTemplate>(),
            cancelSeatRepo ?? new FakeRepository<CancelSeat>(),
            new DepositValidator());

    private static async Task<(FakeRepository<TourDeparture> DepartureRepo, FakeRepository<TourTemplate> TemplateRepo,
        FakeRepository<Customer> CustomerRepo, Guid DepartureId, Guid CustomerId)> SeedAsync(
        int totalSlots, bool closed = false, decimal priceAdult = 1_000_000m, decimal priceChild = 0m)
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var templateRepo = new FakeRepository<TourTemplate>();
        var customerRepo = new FakeRepository<Customer>();

        var template = new TourTemplate { Code = "TPL", Title = "Mẫu", PriceAdult = priceAdult, PriceChild = priceChild };
        await templateRepo.AddAsync(template);
        await templateRepo.SaveChangesAsync();

        var departure = new TourDeparture
        {
            Code = "DEP", Title = "Chuyến", ParentTourId = template.Id, TotalSlots = totalSlots, IsClosed = closed,
        };
        await departureRepo.AddAsync(departure);
        await departureRepo.SaveChangesAsync();

        var customer = new Customer { FullName = "A" };
        await customerRepo.AddAsync(customer);
        await customerRepo.SaveChangesAsync();

        return (departureRepo, templateRepo, customerRepo, departure.Id, customer.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_throws_NotFound_for_missing_departure()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateBookingAsync(Guid.NewGuid(), new CreateBookingDto(Guid.NewGuid(), 1, 0, 0, 0)));
    }

    [Fact]
    public async Task CreateHoldAsync_throws_Validation_when_departure_has_no_template()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var departure = new TourDeparture { Code = "DEP-NOTPL", Title = "Không mẫu", ParentTourId = null, TotalSlots = 10 };
        await departureRepo.AddAsync(departure);
        await departureRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateHoldAsync(departure.Id, new CreateBookingDto(Guid.NewGuid(), 1, 0, 0, 0)));
    }

    [Fact]
    public async Task CreateBookingAsync_computes_TotalRevenue_from_template()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m, priceChild: 3_000_000m);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        var order = await service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 2, 1, 0, 0));

        Assert.Equal(13_000_000m, order.TotalRevenue);   // 2*5tr + 1*3tr
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Booking_over_TotalSlots_is_rejected()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) = await SeedAsync(totalSlots: 2);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        await service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 2, 0, 0, 0));   // đủ 2 chỗ

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0)));
    }

    [Fact]
    public async Task Booking_on_closed_departure_is_rejected()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 10, closed: true);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0)));
    }

    [Fact]
    public async Task Hold_then_confirm_then_deposit_derives_seat_status()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        var held = await service.CreateHoldAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));
        Assert.Equal(SeatStatus.Held, held.Status);
        Assert.NotNull(held.HoldExpiresAt);
        Assert.Equal(5_000_000m, held.LineTotal);

        var confirmed = await service.ConfirmSeatAsync(held.Id);
        Assert.Equal(SeatStatus.HeldConfirmed, confirmed.Status);
        Assert.Null(confirmed.HoldExpiresAt);

        var deposited = await service.DepositAsync(held.Id, new DepositDto(2_000_000m));
        Assert.Equal(SeatStatus.Deposited, deposited.Status);

        var paid = await service.DepositAsync(held.Id, new DepositDto(3_000_000m));
        Assert.Equal(SeatStatus.Paid, paid.Status);
    }

    [Fact]
    public async Task DepositAsync_rejects_non_positive_amount()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) = await SeedAsync(totalSlots: 30);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);
        var held = await service.CreateHoldAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.DepositAsync(held.Id, new DepositDto(0m)));
    }

    [Fact]
    public async Task CancelSeatAsync_blocks_double_cancel()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) = await SeedAsync(totalSlots: 30);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);
        var held = await service.CreateHoldAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));

        var cancelled = await service.CancelSeatAsync(held.Id, new CancelSeatDto("Đổi lịch", 1_000_000m));
        Assert.Equal(SeatStatus.Cancelled, cancelled.Status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CancelSeatAsync(held.Id, new CancelSeatDto(null, 0m)));
    }

    [Fact]
    public async Task Cancelled_seats_do_not_count_toward_capacity()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) = await SeedAsync(totalSlots: 1);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        var order = await service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));
        var lines = await service.ListOrderLinesAsync(order.Id);
        var seatId = Assert.Single(lines).Id;
        await service.CancelSeatAsync(seatId, new CancelSeatDto(null, 0m));

        // Đặt lại 1 chỗ → OK vì chỗ cũ đã huỷ (không tính vào sức chứa)
        var again = await service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));
        Assert.NotEqual(Guid.Empty, again.Id);
    }

    [Fact]
    public async Task AssignSalesAsync_sets_SalesUserId_on_order()
    {
        var orderRepo = new FakeRepository<Order>();
        var order = new Order { Code = "ORD-SALES", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();

        var service = NewService(orderRepo: orderRepo);
        var salesUserId = Guid.NewGuid();

        var updated = await service.AssignSalesAsync(order.Id, new AssignSalesDto(salesUserId));

        Assert.Equal(salesUserId, updated.SalesUserId);
    }

    [Fact]
    public async Task AssignSalesAsync_throws_NotFound_for_missing_order()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignSalesAsync(Guid.NewGuid(), new AssignSalesDto(Guid.NewGuid())));
    }

    [Fact]
    public async Task GetSeatAsync_throws_NotFound_for_missing_seat()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetSeatAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListOrdersAsync_returns_paged_orders()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) = await SeedAsync(totalSlots: 30);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);
        await service.CreateBookingAsync(departureId, new CreateBookingDto(customerId, 1, 0, 0, 0));

        var page = await service.ListOrdersAsync(1, 20);

        Assert.Single(page.Items);
    }
}
