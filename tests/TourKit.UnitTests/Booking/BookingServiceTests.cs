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
        FakeRepository<PaymentVoucher>? paymentRepo = null,
        FakeRepository<MarketType>? marketRepo = null,
        FakeRepository<Invoice>? invoiceRepo = null,
        FakeRepository<SalesOpportunity>? opportunityRepo = null)
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
            paymentRepo ?? new FakeRepository<PaymentVoucher>(),
            marketRepo ?? new FakeRepository<MarketType>(),
            invoiceRepo ?? new FakeRepository<Invoice>(),
            opportunityRepo ?? new FakeRepository<SalesOpportunity>(),
            new FakeOrderQueries(orderRepo ?? new FakeRepository<Order>(), receiptRepo ?? new FakeRepository<ReceiptVoucher>()));

    /// <summary>
    /// Bản giả của IOrderQueries: đọc từ CHÍNH các fake repo của bài test, dựng ra đúng bộ dòng thô
    /// mà bản SQL thật trả về. Nhờ vậy phần LOGIC GỘP trong BookingService vẫn được test như cũ.
    /// </summary>
    private sealed class FakeOrderQueries(
        FakeRepository<Order> orders, FakeRepository<ReceiptVoucher> receipts) : IOrderQueries
    {
        public async Task<IReadOnlyList<OrderStatsRowDto>> GetOrderStatsRowsAsync()
        {
            var paid = (await receipts.ListAsync(r => r.IsRecognized))
                .GroupBy(r => r.OrderId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

            return (await orders.ListAsync())
                .Select(o => new OrderStatsRowDto(
                    (int)o.Status, (int)o.OperationalStatus, o.TotalRevenue, paid.GetValueOrDefault(o.Id)))
                .ToList();
        }
    }

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

    // ---- Chốt cơ hội thành đơn (B3) ----
    // Hệ cũ nối hai thứ theo thứ tự: tạo chỗ + đơn TRƯỚC, đánh dấu phiếu SAU
    // (uspInsertTourSampleCustomer_V4). Đây là lý do cột "Chốt đơn" ở màn Cơ hội không đặt tay được.

    [Fact]
    public async Task Tao_don_kem_co_hoi_thi_danh_dau_co_hoi_da_chot()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var coHoiRepo = new FakeRepository<SalesOpportunity>();
        var coHoi = new SalesOpportunity { Code = "CH-01", Title = "Hỏi Đà Nẵng", ContactName = "Chị Lan" };
        await coHoiRepo.AddAsync(coHoi);
        await coHoiRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo,
            customerRepo: customerRepo, opportunityRepo: coHoiRepo);

        var don = await service.CreateBookingAsync(departureId,
            new CreateBookingDto(customerId, 2, 0, 0, 0, OpportunityId: coHoi.Id));

        var sau = await coHoiRepo.GetByIdAsync(coHoi.Id);
        Assert.Equal(don.Id, sau!.ConvertedOrderId);
        Assert.Equal(OpportunityStageCode.ChotDon, sau.StageCode);
    }

    [Fact]
    public async Task Chot_lai_co_hoi_da_chot_thi_tu_choi()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var coHoiRepo = new FakeRepository<SalesOpportunity>();
        var coHoi = new SalesOpportunity
        {
            Code = "CH-02", Title = "Đã chốt", ContactName = "Anh Bình",
            ConvertedOrderId = Guid.NewGuid(), StageCode = OpportunityStageCode.ChotDon,
        };
        await coHoiRepo.AddAsync(coHoi);
        await coHoiRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo,
            customerRepo: customerRepo, opportunityRepo: coHoiRepo);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateBookingAsync(departureId,
            new CreateBookingDto(customerId, 1, 0, 0, 0, OpportunityId: coHoi.Id)));
    }

    [Fact]
    public async Task Giu_cho_KHONG_danh_dau_co_hoi_da_chot()
    {
        // Giữ chỗ chưa phải chốt deal: khách vẫn bỏ được và chỗ tự nhả khi hết hạn. Đánh dấu chốt ở
        // đây thì phễu đầy cơ hội "đã chốt" mà tiền chưa bao giờ về.
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var coHoiRepo = new FakeRepository<SalesOpportunity>();
        var coHoi = new SalesOpportunity { Code = "CH-03", Title = "Đang giữ chỗ", ContactName = "Chị Mai" };
        await coHoiRepo.AddAsync(coHoi);
        await coHoiRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo,
            customerRepo: customerRepo, opportunityRepo: coHoiRepo);

        await service.CreateHoldAsync(departureId,
            new CreateBookingDto(customerId, 1, 0, 0, 0, OpportunityId: coHoi.Id));

        var sau = await coHoiRepo.GetByIdAsync(coHoi.Id);
        Assert.Null(sau!.ConvertedOrderId);
        Assert.NotEqual(OpportunityStageCode.ChotDon, sau.StageCode);
    }

    [Fact]
    public async Task Co_hoi_da_huy_thi_khong_chot_thanh_don_duoc()
    {
        // Một cơ hội đang nằm trong thống kê "lý do mất khách" mà vẫn sinh ra đơn thật thì hai báo
        // cáo đá nhau, và không bên nào sai rõ ràng để lần ra.
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var coHoiRepo = new FakeRepository<SalesOpportunity>();
        var coHoi = new SalesOpportunity
        {
            Code = "CH-04", Title = "Đã huỷ", ContactName = "Anh Dũng",
            StageCode = OpportunityStageCode.Huy,
        };
        await coHoiRepo.AddAsync(coHoi);
        await coHoiRepo.SaveChangesAsync();

        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo,
            customerRepo: customerRepo, opportunityRepo: coHoiRepo);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateBookingAsync(departureId,
            new CreateBookingDto(customerId, 1, 0, 0, 0, OpportunityId: coHoi.Id)));
    }

    [Fact]
    public async Task Co_hoi_khong_ton_tai_thi_tu_choi_tao_don()
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 30, priceAdult: 5_000_000m);
        var service = NewService(departureRepo: departureRepo, templateRepo: templateRepo, customerRepo: customerRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateBookingAsync(departureId,
            new CreateBookingDto(customerId, 1, 0, 0, 0, OpportunityId: Guid.NewGuid())));
    }

    // ---- Phân trang: cắt ở SQL hay nạp cả bảng rồi cắt trong bộ nhớ ----
    //
    // Đây là bài canh chống TÁI PHÁT. Nạp cả bảng vẫn ra ĐÚNG kết quả, chỉ chậm dần theo lượng dữ
    // liệu — nên không bài kiểm thử nào về mặt nghiệp vụ bắt được. Phải đo bằng số dòng nạp về.

    private static async Task<(FakeRepository<Order> Orders, Guid DepId, Guid CustId,
        FakeRepository<TourDeparture> Dep, FakeRepository<TourTemplate> Tpl, FakeRepository<Customer> Cus)>
        SeedNhieuDonAsync(int soDon)
    {
        var (departureRepo, templateRepo, customerRepo, departureId, customerId) =
            await SeedAsync(totalSlots: 10_000, priceAdult: 1_000_000m);

        var orderRepo = new FakeRepository<Order>();
        for (var i = 0; i < soDon; i++)
        {
            await orderRepo.AddAsync(new Order
            {
                Code = $"OD-{i:0000}", TourDepartureId = departureId, CustomerId = customerId,
                Status = OrderStatus.Confirmed, TotalRevenue = 1_000_000m,
            });
        }
        await orderRepo.SaveChangesAsync();

        return (orderRepo, departureId, customerId, departureRepo, templateRepo, customerRepo);
    }

    [Fact]
    public async Task Danh_sach_don_KHONG_loc_dac_biet_thi_chi_nap_dung_mot_trang()
    {
        var (orderRepo, _, _, dep, tpl, cus) = await SeedNhieuDonAsync(200);
        var service = NewService(departureRepo: dep, templateRepo: tpl, customerRepo: cus, orderRepo: orderRepo);

        var truoc = orderRepo.SoDongDaNap;
        var kq = await service.ListOrdersAsync(1, 20);

        Assert.Equal(20, kq.Items.Count);
        Assert.Equal(200, kq.Total);

        // Cắt trang ở SQL thì KHÔNG dòng đơn nào đi qua ListAsync. Ngưỡng để rộng một chút phòng
        // khi sau này thêm một lượt tra phụ, nhưng vẫn chặn được kiểu nạp cả 200 dòng.
        var daNap = orderRepo.SoDongDaNap - truoc;
        Assert.True(daNap <= 20,
            $"nạp {daNap} dòng đơn cho một trang 20 — đang kéo cả bảng về rồi mới cắt trang");
    }

    [Fact]
    public async Task Trang_hai_van_ra_dung_dong_khi_di_duong_nhanh()
    {
        // Bẫy của đường nhanh: cắt trang ở SQL rồi lỡ cắt LẦN NỮA trong bộ nhớ thì mọi trang sau
        // trang 1 đều rỗng — mà tổng số vẫn đúng, nên nhìn thanh phân trang không thấy gì lạ.
        var (orderRepo, _, _, dep, tpl, cus) = await SeedNhieuDonAsync(50);
        var service = NewService(departureRepo: dep, templateRepo: tpl, customerRepo: cus, orderRepo: orderRepo);

        var t1 = await service.ListOrdersAsync(1, 20);
        var t2 = await service.ListOrdersAsync(2, 20);
        var t3 = await service.ListOrdersAsync(3, 20);

        Assert.Equal(20, t1.Items.Count);
        Assert.Equal(20, t2.Items.Count);
        Assert.Equal(10, t3.Items.Count);

        // Ba trang không được trùng dòng nào.
        var ma = t1.Items.Concat(t2.Items).Concat(t3.Items).Select(x => x.Code).ToList();
        Assert.Equal(50, ma.Distinct().Count());
    }

    [Fact]
    public async Task Loc_theo_tu_khoa_van_ra_dung_ket_qua_du_phai_di_duong_cham()
    {
        // Từ khoá tra cả tên khách và tên tour nên buộc phải làm giàu trước rồi mới lọc. Đường chậm
        // vẫn phải ĐÚNG — tối ưu mà đổi kết quả thì không còn là tối ưu.
        var (orderRepo, _, _, dep, tpl, cus) = await SeedNhieuDonAsync(30);
        var service = NewService(departureRepo: dep, templateRepo: tpl, customerRepo: cus, orderRepo: orderRepo);

        var kq = await service.ListOrdersAsync(1, 20, new OrderListFilter(Q: "OD-0007"));

        Assert.Equal(1, kq.Total);
        Assert.Equal("OD-0007", Assert.Single(kq.Items).Code);
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
    public async Task ListOrdersAsync_filters_by_operationalStatus_collaborator_invoiceStatus()
    {
        var ctv = Guid.NewGuid();
        var orderRepo = new FakeRepository<Order>();
        var oRun = new Order { Code = "O-RUN", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), OperationalStatus = OrderOperationalStatus.Running, CollaboratorId = ctv };
        var oUp = new Order { Code = "O-UP", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), OperationalStatus = OrderOperationalStatus.Upcoming };
        await orderRepo.AddAsync(oRun);
        await orderRepo.AddAsync(oUp);
        await orderRepo.SaveChangesAsync();
        var invoiceRepo = new FakeRepository<Invoice>();
        await invoiceRepo.AddAsync(new Invoice { OrderId = oRun.Id, Series = "S", Number = "1", BuyerName = "A", Status = 1 }); // đã phát hành
        await invoiceRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, invoiceRepo: invoiceRepo);

        Assert.Equal("O-RUN", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(OperationalStatus: (int)OrderOperationalStatus.Running))).Items).Code);
        Assert.Equal("O-RUN", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(CollaboratorId: ctv))).Items).Code);
        Assert.Equal("O-RUN", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(InvoiceStatus: 2))).Items).Code); // đã duyệt
        Assert.Equal("O-UP", Assert.Single((await service.ListOrdersAsync(1, 20, new OrderListFilter(InvoiceStatus: 0))).Items).Code); // chưa xuất
    }

    [Fact]
    public async Task ListOrdersAsync_market_filter_includes_child_markets()
    {
        var marketRepo = new FakeRepository<MarketType>();
        var parent = new MarketType { Name = "Miền Tây" };
        var child = new MarketType { Name = "Miền Tây - Cần Thơ", ParentId = parent.Id };
        await marketRepo.AddAsync(parent);
        await marketRepo.AddAsync(child);
        await marketRepo.SaveChangesAsync();
        var orderRepo = new FakeRepository<Order>();
        await orderRepo.AddAsync(new Order { Code = "O-PARENT", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), MarketTypeId = parent.Id });
        await orderRepo.AddAsync(new Order { Code = "O-CHILD", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), MarketTypeId = child.Id });
        await orderRepo.AddAsync(new Order { Code = "O-OTHER", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(), MarketTypeId = Guid.NewGuid() });
        await orderRepo.SaveChangesAsync();
        var service = NewService(orderRepo: orderRepo, marketRepo: marketRepo);

        // Lọc theo thị trường CHA → phải gồm cả đơn thuộc thị trường CON.
        var codes = (await service.ListOrdersAsync(1, 20, new OrderListFilter(MarketTypeId: parent.Id))).Items.Select(i => i.Code).OrderBy(c => c).ToList();
        Assert.Equal(new[] { "O-CHILD", "O-PARENT" }, codes);
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

    // ---- Item B: cổng tất toán/chốt đơn ----

    private static async Task<(FakeRepository<Order> Repo, Order Order)> SeedOrderAsync(
        OrderStatus status = OrderStatus.Confirmed, bool paymentRecognized = true, bool commissionSettled = true)
    {
        var repo = new FakeRepository<Order>();
        var order = new Order
        {
            Code = "ORD-1", CustomerId = Guid.NewGuid(), TourDepartureId = Guid.NewGuid(),
            Status = status, IsPaymentRecognized = paymentRecognized, IsCommissionSettled = commissionSettled,
        };
        await repo.AddAsync(order);
        await repo.SaveChangesAsync();
        return (repo, order);
    }

    [Fact]
    public async Task CloseOrder_du_dieu_kien_thi_tat_toan()
    {
        var (repo, order) = await SeedOrderAsync();
        var service = NewService(orderRepo: repo);
        var userId = Guid.NewGuid();

        var dto = await service.CloseOrderAsync(order.Id, userId);

        Assert.Equal(OrderStatus.Closed, dto.Status);
        var saved = await repo.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Closed, saved!.Status);
        Assert.NotNull(saved.ClosedAt);
        Assert.Equal(userId, saved.ClosedByUserId);
    }

    [Fact]
    public async Task CloseOrder_chan_khi_chua_xac_nhan()
    {
        var (repo, order) = await SeedOrderAsync(status: OrderStatus.Draft);
        var service = NewService(orderRepo: repo);
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CloseOrderAsync(order.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseOrder_chan_khi_chua_ghi_nhan_dong_tien()
    {
        var (repo, order) = await SeedOrderAsync(paymentRecognized: false);
        var service = NewService(orderRepo: repo);
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CloseOrderAsync(order.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseOrder_chan_khi_hoa_hong_chua_quyet()
    {
        var (repo, order) = await SeedOrderAsync(commissionSettled: false);
        var service = NewService(orderRepo: repo);
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CloseOrderAsync(order.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task ReopenOrder_mo_lai_don_da_tat_toan()
    {
        var (repo, order) = await SeedOrderAsync(status: OrderStatus.Closed);
        order.ClosedAt = DateTimeOffset.UtcNow;
        order.ClosedByUserId = Guid.NewGuid();
        await repo.SaveChangesAsync();
        var service = NewService(orderRepo: repo);

        var dto = await service.ReopenOrderAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, dto.Status);
        var saved = await repo.GetByIdAsync(order.Id);
        Assert.Null(saved!.ClosedAt);
        Assert.Null(saved.ClosedByUserId);
    }

    [Fact]
    public async Task AssignSales_chan_khi_don_da_tat_toan()
    {
        var (repo, order) = await SeedOrderAsync(status: OrderStatus.Closed);
        var service = NewService(orderRepo: repo);
        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.AssignSalesAsync(order.Id, new AssignSalesDto(Guid.NewGuid())));
    }
}
