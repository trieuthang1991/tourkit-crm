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
        FakeRepository<CancelSeat>? cancelSeatRepo = null,
        FakeRepository<ReceiptVoucher>? receiptRepo = null,
        FakeRepository<User>? userRepo = null,
        FakeRepository<OrderCost>? orderCostRepo = null,
        FakeRepository<Provider>? providerRepo = null,
        FakeRepository<PaymentVoucher>? paymentRepo = null)
        => new(
            departureRepo ?? new FakeRepository<TourDeparture>(),
            seatRepo ?? new FakeRepository<TourCustomer>(),
            orderRepo ?? new FakeRepository<Order>(),
            customerRepo ?? new FakeRepository<Customer>(),
            templateRepo ?? new FakeRepository<TourTemplate>(),
            cancelSeatRepo ?? new FakeRepository<CancelSeat>(),
            receiptRepo ?? new FakeRepository<ReceiptVoucher>(),
            new DepositValidator(),
            new FakeCurrentUser(),
            userRepo ?? new FakeRepository<User>(),
            orderCostRepo ?? new FakeRepository<OrderCost>(),
            providerRepo ?? new FakeRepository<Provider>(),
            paymentRepo ?? new FakeRepository<PaymentVoucher>());

    private sealed class FakeCurrentUser : TourKit.Shared.Security.ICurrentUserContext
    {
        public Guid? UserId => null;
    }

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

    [Fact]
    public async Task ListOrdersAsync_filters_by_status()
    {
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "D1", Status = OrderStatus.Draft, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() });
        await orderRepo.AddAsync(new Order { Code = "C1", Status = OrderStatus.Confirmed, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() });
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo);

        var page = await service.ListOrdersAsync(1, 20, new OrderListFilter(Status: (int)OrderStatus.Confirmed));

        Assert.Equal("C1", Assert.Single(page.Items).Code);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_bookingType_market_group_commission()
    {
        var market = Guid.NewGuid();
        var group = Guid.NewGuid();
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "O-FIT", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), BookingType = 0, MarketTypeId = market, TourGroupId = group, IsCommissionSettled = true });
        await orderRepo.AddAsync(new Order { Code = "O-GIT", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), BookingType = 1, IsCommissionSettled = false });
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo);

        Assert.Equal("O-FIT", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(BookingType: 0))).Items).Code);
        Assert.Equal("O-FIT", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(MarketTypeId: market))).Items).Code);
        Assert.Equal("O-FIT", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(TourGroupId: group))).Items).Code);
        Assert.Equal("O-GIT", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(CommissionSettled: false))).Items).Code);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_tourType_via_departure()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var depIn = new TourDeparture { Code = "DEP-IN", Title = "Chuyến inbound", TourType = "inbound" };
        var depOut = new TourDeparture { Code = "DEP-OUT", Title = "Chuyến outbound", TourType = "outbound" };
        await departureRepo.AddAsync(depIn);
        await departureRepo.AddAsync(depOut);
        await departureRepo.SaveChangesAsync();
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "O-IN", CustomerId = Guid.NewGuid(), TourDepartureId = depIn.Id });
        await orderRepo.AddAsync(new Order { Code = "O-OUT", CustomerId = Guid.NewGuid(), TourDepartureId = depOut.Id });
        await orderRepo.SaveChangesAsync();
        var service = NewService(departureRepo: departureRepo, orderRepo: orderRepo);

        Assert.Equal("O-IN", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(TourType: "inbound"))).Items).Code);
    }

    [Fact]
    public async Task GetOrderFilterOptionsAsync_returns_distinct_tourTypes_from_orders()
    {
        var departureRepo = new FakeRepository<TourDeparture>();
        var depIn = new TourDeparture { Code = "DEP-IN", Title = "IN", TourType = "inbound" };
        var depOut = new TourDeparture { Code = "DEP-OUT", Title = "OUT", TourType = "outbound" };
        var depOrphan = new TourDeparture { Code = "DEP-NONE", Title = "NONE", TourType = "domestic" };
        await departureRepo.AddAsync(depIn);
        await departureRepo.AddAsync(depOut);
        await departureRepo.AddAsync(depOrphan);
        await departureRepo.SaveChangesAsync();
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "O1", CustomerId = Guid.NewGuid(), TourDepartureId = depIn.Id });
        await orderRepo.AddAsync(new Order { Code = "O2", CustomerId = Guid.NewGuid(), TourDepartureId = depOut.Id });
        await orderRepo.SaveChangesAsync();
        var service = NewService(departureRepo: departureRepo, orderRepo: orderRepo);

        var opts = await service.GetOrderFilterOptionsAsync();

        // Chỉ loại tour của chuyến ĐANG có đơn (domestic không có đơn → không xuất hiện).
        Assert.Equal(new[] { "inbound", "outbound" }, opts.TourTypes);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_provider_via_orderCost()
    {
        var provA = Guid.NewGuid();
        var orderRepo = new FakeRepository<Order>();
        var oA = new Order { Code = "O-A", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        var oB = new Order { Code = "O-B", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        await orderRepo.AddAsync(oA);
        await orderRepo.AddAsync(oB);
        await orderRepo.SaveChangesAsync();
        var orderCostRepo = new FakeRepository<OrderCost>();
        await orderCostRepo.AddAsync(new OrderCost { OrderId = oA.Id, ProviderId = provA });
        await orderCostRepo.AddAsync(new OrderCost { OrderId = oB.Id, ProviderId = Guid.NewGuid() });
        await orderCostRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, orderCostRepo: orderCostRepo);

        Assert.Equal("O-A", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(ProviderId: provA))).Items).Code);
    }

    [Fact]
    public async Task GetOrderFilterOptionsAsync_returns_providers_present_in_orders()
    {
        var provA = Guid.NewGuid();
        var orderRepo = new FakeRepository<Order>();
        var oA = new Order { Code = "O-A", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        await orderRepo.AddAsync(oA);
        await orderRepo.SaveChangesAsync();
        var providerRepo = new FakeRepository<Provider>();
        var pA = new Provider { Code = "NCC-A", Name = "Khách sạn A" };
        var pUnused = new Provider { Code = "NCC-Z", Name = "Không dùng" };
        await providerRepo.AddAsync(pA);
        await providerRepo.AddAsync(pUnused);
        await providerRepo.SaveChangesAsync();
        var orderCostRepo = new FakeRepository<OrderCost>();
        await orderCostRepo.AddAsync(new OrderCost { OrderId = oA.Id, ProviderId = pA.Id });
        await orderCostRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, orderCostRepo: orderCostRepo, providerRepo: providerRepo);

        var opts = await service.GetOrderFilterOptionsAsync();

        var p = Assert.Single(opts.Providers);
        Assert.Equal("Khách sạn A", p.Name);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_q_by_code_and_customerName()
    {
        var customerRepo = new FakeRepository<Customer>();
        var cust = new Customer { FullName = "Nguyễn Bình" };
        await customerRepo.AddAsync(cust);
        await customerRepo.SaveChangesAsync();
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "OD_777", CustomerId = cust.Id, TourDepartureId = Guid.NewGuid() });
        await orderRepo.AddAsync(new Order { Code = "OD_999", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() });
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, customerRepo: customerRepo);

        Assert.Equal("OD_777", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(Q: "Bình"))).Items).Code);
        Assert.Equal("OD_777", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(Q: "OD_777"))).Items).Code);
    }

    [Fact]
    public async Task GetOrderStatsAsync_sums_revenue_paid_and_counts_status()
    {
        var orderRepo = new FakeRepository<Order>();
        var o1 = new Order { Code = "A", Status = OrderStatus.Confirmed, TotalRevenue = 5_000_000m, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        var o2 = new Order { Code = "B", Status = OrderStatus.Cancelled, TotalRevenue = 3_000_000m, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        await orderRepo.AddAsync(o1);
        await orderRepo.AddAsync(o2);
        await orderRepo.SaveChangesAsync();
        var receiptRepo = new FakeRepository<ReceiptVoucher>();
        await receiptRepo.AddAsync(new ReceiptVoucher { OrderId = o1.Id, Amount = 2_000_000m, IsRecognized = true });
        await receiptRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, receiptRepo: receiptRepo);

        var stats = await service.GetOrderStatsAsync();

        Assert.Equal(2, stats.Total);
        Assert.Equal(8_000_000m, stats.TotalRevenue);
        Assert.Equal(2_000_000m, stats.TotalPaid);
        Assert.Equal(6_000_000m, stats.TotalOutstanding); // (5M-2M) + (3M-0)
        Assert.Equal(1, stats.Confirmed);
        Assert.Equal(1, stats.Cancelled);
        Assert.Equal(0, stats.Draft);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_paymentStatus_and_stats_buckets()
    {
        var orderRepo = new FakeRepository<Order>();
        var receiptRepo = new FakeRepository<ReceiptVoucher>();
        var unpaid = new Order { Code = "U", TotalRevenue = 10_000_000m, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        var deposit = new Order { Code = "D", TotalRevenue = 10_000_000m, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        var paid = new Order { Code = "P", TotalRevenue = 10_000_000m, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        await orderRepo.AddAsync(unpaid);
        await orderRepo.AddAsync(deposit);
        await orderRepo.AddAsync(paid);
        await orderRepo.SaveChangesAsync();
        await receiptRepo.AddAsync(new ReceiptVoucher { OrderId = deposit.Id, Amount = 3_000_000m, IsRecognized = true });
        await receiptRepo.AddAsync(new ReceiptVoucher { OrderId = paid.Id, Amount = 10_000_000m, IsRecognized = true });
        await receiptRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, receiptRepo: receiptRepo);

        Assert.Equal("U", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(PaymentStatus: 0))).Items).Code);
        Assert.Equal("D", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(PaymentStatus: 1))).Items).Code);
        Assert.Equal("P", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(PaymentStatus: 2))).Items).Code);

        var stats = await service.GetOrderStatsAsync();
        Assert.Equal(1, stats.Unpaid);
        Assert.Equal(1, stats.Deposit);
        Assert.Equal(1, stats.Paid);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_branch_sales_and_createdRange()
    {
        var orderRepo = new FakeRepository<Order>();
        var branchA = Guid.NewGuid();
        var sales = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var match = new Order { Code = "M", BranchId = branchA, SalesUserId = sales, CreatedByUserId = creator, CreatedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        var other = new Order { Code = "O", BranchId = Guid.NewGuid(), CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() };
        await orderRepo.AddAsync(match);
        await orderRepo.AddAsync(other);
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo);

        Assert.Equal("M", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(BranchId: branchA))).Items).Code);
        Assert.Equal("M", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(SalesUserId: sales))).Items).Code);
        Assert.Equal("M", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(CreatedByUserId: creator))).Items).Code);
        Assert.Equal("M", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(CreatedFrom: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)))).Items).Code);
    }

    [Fact]
    public async Task ListOrdersAsync_filters_by_department_via_sales_user()
    {
        var orderRepo = new FakeRepository<Order>();
        var userRepo = new FakeRepository<User>();
        var deptA = Guid.NewGuid();
        var salesUser = new User { FullName = "Sales A", DepartmentId = deptA };
        await userRepo.AddAsync(salesUser);
        await userRepo.SaveChangesAsync();
        await orderRepo.AddAsync(new Order { Code = "M", SalesUserId = salesUser.Id, CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() });
        await orderRepo.AddAsync(new Order { Code = "O", SalesUserId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid() });
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, userRepo: userRepo);

        Assert.Equal("M", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(DepartmentId: deptA))).Items).Code);
    }
}
