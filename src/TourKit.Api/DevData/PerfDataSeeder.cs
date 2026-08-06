using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Api.DevData;

/// <summary>
/// DEV-ONLY bulk "performance" seeder — bơm HÀNG NGHÌN dòng vào các bảng CRM (khách/lead/đơn/phiếu/báo giá…)
/// để test thật hiệu năng lưới/phân trang/truy vấn. Chạy sau <see cref="DemoDataSeeder"/> và TÁI SỬ DỤNG danh mục
/// nó đã tạo (chi nhánh/phòng ban/NV/NCC/chuyến/…). KHÔNG thay đổi schema — chỉ INSERT bằng bảng sẵn có.
///
/// Kích hoạt qua biến môi trường SEED_PERF_COUNT (xem Program.cs). Idempotent: đã seed (>500 KH) → bỏ qua.
/// Ghi theo LÔ ~500 entity → SaveChanges → ChangeTracker.Clear() để giữ RAM phẳng. Random cố định (12345) → tái tạo.
///
/// Lưu ý: interceptor audit tự sinh 1 <see cref="ActivityLog"/> mỗi lần INSERT khi có tenant → bảng ActivityLog
/// được nạp KHỐI LƯỢNG LỚN "miễn phí" (nhiều hơn scale), nên seeder này KHÔNG seed ActivityLog thủ công.
/// Ngoài ra <see cref="AppDbContext.SaveChanges"/> ép CreatedAt = now khi INSERT → ta rải các cột NGÀY nghiệp vụ
/// (IssuedAt/InvoiceDate/DepartDate/RemindAt/DueDate…) đúng cách, và backdate CreatedAt bằng SQL best-effort ở cuối.
/// </summary>
public static class PerfDataSeeder
{
    private const int BatchSize = 500;
    private const int SeededGuard = 500;

    public static async Task SeedAsync(AppDbContext db, AmbientTenantContext ambient, int scale)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == DemoDataSeeder.DemoSlug && !t.IsDeleted);
        if (tenant is null)
        {
            Console.WriteLine("[PerfSeed] Không tìm thấy tenant demo — bỏ qua (DemoDataSeeder chạy trước sẽ tạo).");
            return;
        }

        // Scope theo tenant demo (interceptor tự gán TenantId + tự ghi ActivityLog khi lưu).
        ambient.SetTenant(tenant.Id);

        // Idempotency: đã seed rồi (đủ nhiều KH) → không seed lại mỗi lần khởi động.
        var existingCustomers = await db.Set<Customer>().CountAsync();
        if (existingCustomers > SeededGuard)
        {
            Console.WriteLine($"[PerfSeed] Bỏ qua — đã có {existingCustomers} khách hàng (>{SeededGuard}, coi như đã seed perf).");
            return;
        }

        if (scale < 1)
        {
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine($"[PerfSeed] BẮT ĐẦU seed hiệu năng scale={scale} vào tenant '{tenant.Slug}'.");

        var rng = new Random(12345);
        var now = DateTimeOffset.UtcNow;

        // ---- Dữ liệu tĩnh (không dùng NuGet: mảng hardcode) ----------------------
        string[] ho = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"];
        string[] dem = ["Văn", "Thị", "Hữu", "Đức", "Minh", "Thành", "Thu", "Ngọc", "Gia", "Bảo", "Anh", "Quốc", "Hải", "Kim", "Xuân"];
        string[] ten = ["An", "Bình", "Cường", "Dũng", "Dung", "Hà", "Khôi", "Lan", "Mai", "Nam", "Ngọc", "Phúc", "Quân", "Sơn", "Tú", "Uyên", "Vân", "Yến", "Long", "Hùng"];
        string[] cities = ["Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", "Nha Trang", "Đà Lạt", "Huế", "Vũng Tàu", "Quảng Ninh", "Hạ Long", "Hội An", "Quy Nhơn", "Buôn Ma Thuột"];
        string[] streets = ["Lê Lợi", "Trần Hưng Đạo", "Nguyễn Huệ", "Hai Bà Trưng", "Lý Thường Kiệt", "Phan Chu Trinh", "Hùng Vương", "Bà Triệu", "Nguyễn Trãi", "Điện Biên Phủ"];
        string[] sources = ["Facebook", "Website", "Giới thiệu", "Zalo", "Google", "TikTok", "Hotline"];
        string[] tags = ["VIP", "Thân thiết", "Tiềm năng", "Doanh nghiệp"];
        string[] methods = ["cash", "bank", "momo", "vnpay"];
        string[] tourTypes = ["inbound", "outbound", "domestic"];
        string[] routes = ["SGN-HAN", "HAN-DAD", "SGN-DAD", "HAN-SGN-HAN", "SGN-BKK-SGN", "HAN-ICN", "SGN-SIN"];

        string Name() => $"{ho[rng.Next(ho.Length)]} {dem[rng.Next(dem.Length)]} {ten[rng.Next(ten.Length)]}";
        string Phone() => $"09{rng.Next(0, 100_000_000):D8}";
        string City() => cities[rng.Next(cities.Length)];
        string Address() => $"{rng.Next(1, 400)} {streets[rng.Next(streets.Length)]}, {City()}";
        string Pick(string[] a) => a[rng.Next(a.Length)];
        decimal Money(int minK, int maxK) => rng.Next(minK, maxK) * 1000m;              // bội số 1.000đ
        DateTimeOffset PastDate(int maxDaysAgo) => now.AddDays(-rng.Next(0, maxDaysAgo)).AddMinutes(-rng.Next(0, 1440));
        DateTimeOffset SpreadDate(int minDays, int maxDays) => now.AddDays(rng.Next(minDays, maxDays));
        T PickId<T>(IReadOnlyList<T> a) => a[rng.Next(a.Count)];

        // ---- Lô ghi: gom ~BatchSize entity → lưu → clear tracker ------------------
        var buf = 0;
        var savedSinceLog = 0;
        async Task Add(object entity)
        {
            db.Add(entity);
            if (++buf >= BatchSize)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                savedSinceLog += buf;
                buf = 0;
            }
        }
        async Task Flush(string label, int total)
        {
            if (buf > 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                savedSinceLog += buf;
                buf = 0;
            }
            Console.WriteLine($"[PerfSeed] {label}: {total} dòng (t={sw.Elapsed.TotalSeconds:F1}s).");
            savedSinceLog = 0;
        }

        // ---- 1) Danh mục: tái sử dụng của DemoDataSeeder; nếu trống thì tạo tối thiểu ----
        var branchIds = await db.Set<Branch>().Select(x => x.Id).ToListAsync();
        if (branchIds.Count == 0)
        {
            var b1 = new Branch { Code = "CN-HN", Name = "Chi nhánh Hà Nội", SortOrder = 1 };
            var b2 = new Branch { Code = "CN-HCM", Name = "Chi nhánh TP.HCM", SortOrder = 2 };
            db.AddRange(b1, b2);
            await db.SaveChangesAsync();
            branchIds = [b1.Id, b2.Id];
        }

        var marketIds = await db.Set<MarketType>().Select(x => x.Id).ToListAsync();
        if (marketIds.Count == 0)
        {
            var m1 = new MarketType { Name = "Inbound", SortOrder = 1 };
            var m2 = new MarketType { Name = "Outbound", SortOrder = 2 };
            var m3 = new MarketType { Name = "Nội địa", SortOrder = 3 };
            db.AddRange(m1, m2, m3);
            await db.SaveChangesAsync();
            marketIds = [m1.Id, m2.Id, m3.Id];
        }

        var tourGroupIds = await db.Set<TourGroup>().Select(x => x.Id).ToListAsync();

        var userIds = await db.Set<User>().Select(x => x.Id).ToListAsync();
        if (userIds.Count == 0)
        {
            var users = Enumerable.Range(1, 4)
                .Select(i => new User { Email = $"perfsales{i}@demo.vn", FullName = Name(), PasswordHash = "!perf-seed-no-login" })
                .ToList();
            db.AddRange(users);
            await db.SaveChangesAsync();
            userIds = users.Select(u => u.Id).ToList();
        }

        var providerIds = await db.Set<Provider>().Select(x => x.Id).ToListAsync();
        if (providerIds.Count == 0)
        {
            var provs = new[]
            {
                new Provider { Code = "NCC_P1", Name = "Khách sạn Perf 1", Type = ProviderType.Hotel, Province = "Hà Nội", Rate = 4 },
                new Provider { Code = "NCC_P2", Name = "Vận tải Perf 2", Type = ProviderType.Vehicle, Province = "Hồ Chí Minh", Rate = 4 },
                new Provider { Code = "NCC_P3", Name = "Hãng bay Perf 3", Type = ProviderType.Airline, Province = "Hà Nội", Rate = 5 },
            };
            db.AddRange(provs);
            await db.SaveChangesAsync();
            providerIds = provs.Select(p => p.Id).ToList();
        }

        // ---- 2) ~50 chuyến khởi hành rải ngày (quá khứ ↔ tương lai) --------------
        const int extraDepartures = 50;
        for (var i = 0; i < extraDepartures; i++)
        {
            var daysToGo = rng.Next(-200, 200);
            await Add(new TourDeparture
            {
                Code = $"DEP_PERF_{i:D3}",
                Title = $"Chuyến perf {i:D3} — {City()}",
                TourType = Pick(tourTypes),
                DepartureDate = now.AddDays(daysToGo),
                EndDate = now.AddDays(daysToGo + rng.Next(2, 6)),
                TotalSlots = rng.Next(20, 45),
                AssignedToUserId = PickId(userIds),
                Status = 1,
            });
        }
        await Flush("TourDeparture (extra)", extraDepartures);

        var departureIds = await db.Set<TourDeparture>().Select(x => x.Id).ToListAsync();

        // ---- 3) Khách hàng: `scale` dòng -----------------------------------------
        for (var i = 1; i <= scale; i++)
        {
            await Add(new Customer
            {
                Code = $"KHP{i:D6}",
                FullName = Name(),
                Phone = Phone(),
                CustomerType = rng.Next(0, 4),
                Source = Pick(sources),
                Tag = rng.Next(0, 3) == 0 ? Pick(tags) : null,
                Address = Address(),
            });
        }
        await Flush("Customer", scale);

        var customerIds = await db.Set<Customer>()
            .Where(c => c.Code != null && c.Code.StartsWith("KHP"))
            .Select(c => c.Id).ToListAsync();

        // ---- 4) Lead: ~`scale` dòng ----------------------------------------------
        var leadStatuses = new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Won, LeadStatus.Lost };
        for (var i = 0; i < scale; i++)
        {
            await Add(new Lead
            {
                FullName = Name(),
                Phone = Phone(),
                Email = rng.Next(0, 2) == 0 ? $"lead{i}@mail.vn" : null,
                Source = Pick(sources),
                Status = leadStatuses[rng.Next(leadStatuses.Length)],
                AssignedToUserId = PickId(userIds),
                CreatedByUserId = PickId(userIds),
                BranchId = PickId(branchIds),
            });
        }
        await Flush("Lead", scale);

        // ---- 5) Chăm sóc KH: ~`scale/2` -----------------------------------------
        var careCount = scale / 2;
        for (var i = 0; i < careCount; i++)
        {
            await Add(new CustomerCare
            {
                CustomerId = PickId(customerIds),
                Title = $"Chăm sóc #{i} — {Pick(["Gọi xác nhận", "Nhắc thanh toán", "Feedback sau tour", "Ưu đãi mới"])}",
                Detail = "Nội dung chăm sóc tự sinh cho test hiệu năng.",
                RemindAt = now.AddDays(rng.Next(-30, 30)),
                AssignedToUserId = PickId(userIds),
                Status = rng.Next(0, 3),
            });
        }
        await Flush("CustomerCare", careCount);

        // ---- 6) Đơn hàng: `scale` -------------------------------------------------
        var orderStatuses = new[] { OrderStatus.Draft, OrderStatus.Confirmed, OrderStatus.Cancelled };
        var opsStatuses = new[]
        {
            OrderOperationalStatus.Upcoming, OrderOperationalStatus.Running, OrderOperationalStatus.PendingSettlement,
            OrderOperationalStatus.Settled, OrderOperationalStatus.Done, OrderOperationalStatus.Cancelled,
        };
        for (var i = 1; i <= scale; i++)
        {
            var revenue = Money(2_000, 60_000);
            var cost = Math.Round(revenue * (decimal)(0.55 + rng.NextDouble() * 0.3), 0);
            await Add(new Order
            {
                Code = $"ODP{i:D6}",
                TourDepartureId = PickId(departureIds),
                CustomerId = PickId(customerIds),
                BranchId = PickId(branchIds),
                SalesUserId = PickId(userIds),
                CreatedByUserId = PickId(userIds),
                MarketTypeId = PickId(marketIds),
                TourGroupId = tourGroupIds.Count > 0 && rng.Next(0, 2) == 0 ? PickId(tourGroupIds) : null,
                BookingType = rng.Next(0, 7),
                IsCommissionSettled = rng.Next(0, 2) == 1,
                Status = orderStatuses[rng.Next(orderStatuses.Length)],
                OperationalStatus = opsStatuses[rng.Next(opsStatuses.Length)],
                TotalRevenue = revenue,
                TotalCost = cost,
                ApprovedRevenue = rng.Next(0, 2) == 0 ? revenue : 0m,
                IsPaymentRecognized = rng.Next(0, 2) == 1,
            });
        }
        await Flush("Order", scale);

        var orderIds = await db.Set<Order>()
            .Where(o => o.Code.StartsWith("ODP"))
            .Select(o => o.Id).ToListAsync();

        // ---- 7) Chi phí NCC + phụ thu: vài dòng cho ~1/3 số đơn ------------------
        var costRows = 0;
        var surRows = 0;
        for (var i = 0; i < orderIds.Count; i += 3)
        {
            var oid = orderIds[i];
            var lines = rng.Next(1, 4);
            for (var k = 0; k < lines; k++)
            {
                await Add(new OrderCost
                {
                    OrderId = oid,
                    ProviderId = PickId(providerIds),
                    ServiceName = Pick(["Phòng khách sạn", "Xe đưa đón", "Vé máy bay", "Hướng dẫn viên", "Ăn uống", "Vé tham quan"]),
                    DayIndex = k + 1,
                    ExpectedAmount = Money(500, 10_000),
                    ActualAmount = Money(500, 10_000),
                    Status = rng.Next(0, 2),
                });
                costRows++;
            }
            if (rng.Next(0, 2) == 0)
            {
                var isPercent = rng.Next(0, 2) == 1;
                await Add(new OrderSurcharge
                {
                    OrderId = oid,
                    Description = isPercent ? "Phụ thu cao điểm" : "Phụ thu phòng đơn",
                    CalcType = isPercent ? 1 : 0,
                    Value = isPercent ? 10m : 500_000m,
                    Amount = Money(200, 2_000),
                });
                surRows++;
            }
        }
        await Flush($"OrderCost ({costRows}) + OrderSurcharge ({surRows})", costRows + surRows);

        // ---- 7c) Đánh giá tour (feedback): ~scale/3, GẮN đơn+chuyến để NVPT/NVĐH/ngày/mã đặt chỗ hiện đủ.
        // Phân bố sao ĐÃ BIẾT (45/25/15/10/5% cho 5→1★) để đối chiếu 7 thẻ thống kê — lệch là lộ bug ngay.
        var ratingSrc = await db.Set<Order>()
            .Where(o => o.Code.StartsWith("ODP"))
            .OrderBy(o => o.Code)
            .Take(Math.Max(1, scale / 3))
            .Select(o => new { o.Id, o.TourDepartureId, o.SalesUserId })
            .ToListAsync();
        int[] starWeights = [5, 5, 5, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4, 4, 3, 3, 3, 2, 2, 1]; // 9/5/3/2/1 trên 20
        string[] ratingComments =
            ["Tour rất tốt, hướng dẫn nhiệt tình", "Khách sạn ổn, ăn ngon", "Xe hơi cũ", "Sẽ quay lại lần sau",
             "Lịch trình hơi gấp", "Rất hài lòng, đáng tiền", "Bình thường", "Cần cải thiện dịch vụ"];
        var ratingRows = 0;
        foreach (var o in ratingSrc)
        {
            await Add(new TourRating
            {
                TourDepartureId = o.TourDepartureId,
                OrderId = o.Id,
                CustomerName = Name(),
                CustomerPhone = Phone(),
                Stars = starWeights[rng.Next(starWeights.Length)],
                Comment = Pick(ratingComments),
                Status = rng.Next(0, 100) < 85 ? 1 : 0,     // 85% Hiển thị · 15% Ẩn
                SalesUserId = o.SalesUserId,                // NVPT khớp NV phụ trách của đơn (đối chiếu được)
                OperatorUserId = PickId(userIds),           // NVĐH ngẫu nhiên (lọc được)
            });
            ratingRows++;
        }
        await Flush("TourRating", ratingRows);

        // ---- 8) Báo giá + dòng báo giá: `scale` quote × 2–4 dòng -----------------
        var quoteLineRows = 0;
        for (var i = 1; i <= scale; i++)
        {
            var custId = PickId(customerIds);
            var total = Money(2_000, 40_000);
            var qcost = Math.Round(total * (decimal)(0.5 + rng.NextDouble() * 0.3), 0);
            var quote = new Quote
            {
                Code = $"BGP{i:D6}",
                QuoteType = rng.Next(0, 7),
                CustomerId = custId,
                CustomerName = Name(),
                Title = $"Báo giá perf #{i} — {City()}",
                ValidUntil = now.AddDays(rng.Next(-10, 30)),
                Status = rng.Next(0, 4),
                TotalAmount = total,
                TotalCost = qcost,
                TotalProfit = total - qcost,
                Adults = rng.Next(1, 20),
                Children = rng.Next(0, 5),
                Infants = rng.Next(0, 2),
            };
            await Add(quote);
            var nLines = rng.Next(2, 5);
            for (var k = 0; k < nLines; k++)
            {
                var qty = rng.Next(1, 6);
                var unit = Money(300, 8_000);
                await Add(new QuoteLine
                {
                    QuoteId = quote.Id,
                    Description = Pick(["Phòng nghỉ", "Vận chuyển", "Ăn uống", "Vé tham quan", "HDV", "Bảo hiểm"]),
                    Quantity = qty,
                    UnitPrice = unit,
                    UnitCost = Math.Round(unit * 0.8m, 0),
                    MarginPercent = 20m,
                });
                quoteLineRows++;
            }
        }
        await Flush($"Quote ({scale}) + QuoteLine ({quoteLineRows})", scale + quoteLineRows);

        // ---- 9) Phiếu thu: `scale` (gắn đơn ngẫu nhiên) --------------------------
        for (var i = 1; i <= scale; i++)
        {
            var status = rng.Next(0, 3);
            await Add(new ReceiptVoucher
            {
                Code = $"PTP{i:D6}",
                OrderId = PickId(orderIds),
                Amount = Money(500, 30_000),
                PaymentMethod = Pick(methods),
                Partner = Name(),
                Status = status,
                IsRecognized = status == 1,
                IssuedAt = PastDate(365),
            });
        }
        await Flush("ReceiptVoucher", scale);

        // ---- 10) Phiếu chi: `scale` (gắn đơn + NCC) ------------------------------
        for (var i = 1; i <= scale; i++)
        {
            var status = rng.Next(0, 3);
            await Add(new PaymentVoucher
            {
                Code = $"PCP{i:D6}",
                OrderId = PickId(orderIds),
                ProviderId = PickId(providerIds),
                Amount = Money(500, 25_000),
                PaymentMethod = Pick(methods),
                Partner = Name(),
                Status = status,
                IsRecognized = status == 1,
                IssuedAt = PastDate(365),
            });
        }
        await Flush("PaymentVoucher", scale);

        // ---- 11) Hoá đơn VAT + dòng: `scale/2` -----------------------------------
        var invoiceCount = scale / 2;
        var invoiceLineRows = 0;
        for (var i = 1; i <= invoiceCount; i++)
        {
            var invoice = new Invoice
            {
                Series = "1C26TK",
                Number = $"{i:D7}",
                InvoiceDate = PastDate(365),
                OrderId = rng.Next(0, 2) == 0 ? PickId(orderIds) : null,
                BuyerName = Name(),
                BuyerTaxCode = rng.Next(0, 2) == 0 ? rng.Next(100000000, 999999999).ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                Status = rng.Next(0, 3),
            };
            var nLines = rng.Next(1, 4);
            decimal sub = 0m, vat = 0m;
            var lines = new List<InvoiceLine>();
            for (var k = 0; k < nLines; k++)
            {
                var qty = rng.Next(1, 5);
                var unit = Money(500, 12_000);
                var rate = new[] { 0m, 5m, 8m, 10m }[rng.Next(0, 4)];
                sub += qty * unit;
                vat += qty * unit * rate / 100m;
                lines.Add(new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    Description = Pick(["Dịch vụ tour", "Phòng khách sạn", "Vé máy bay", "Vận chuyển", "Ăn uống"]),
                    Quantity = qty,
                    UnitPrice = unit,
                    VatRate = rate,
                });
            }
            invoice.Subtotal = sub;
            invoice.VatAmount = vat;
            invoice.TotalAmount = sub + vat;
            await Add(invoice);
            foreach (var l in lines)
            {
                await Add(l);
                invoiceLineRows++;
            }
        }
        await Flush($"Invoice ({invoiceCount}) + InvoiceLine ({invoiceLineRows})", invoiceCount + invoiceLineRows);

        // ---- 12) Booking dịch vụ lẻ: `scale/2` -----------------------------------
        var svcCount = scale / 2;
        var svcTypes = new[] { ServiceBookingType.Hotel, ServiceBookingType.Flight, ServiceBookingType.Visa, ServiceBookingType.Ticket, ServiceBookingType.Transfer };
        for (var i = 1; i <= svcCount; i++)
        {
            var qty = rng.Next(1, 6);
            var unit = Money(500, 6_000);
            var totalAmt = qty * unit;
            var start = SpreadDate(-60, 120);
            await Add(new ServiceBooking
            {
                Code = $"DVP{i:D6}",
                Type = svcTypes[rng.Next(svcTypes.Length)],
                OrderId = rng.Next(0, 2) == 0 ? PickId(orderIds) : null,
                ProviderId = PickId(providerIds),
                Description = $"Dịch vụ perf #{i}",
                StartDate = start,
                EndDate = start.AddDays(rng.Next(1, 5)),
                Quantity = qty,
                UnitPrice = unit,
                TotalAmount = totalAmt,
                PaidAmount = Math.Round(totalAmt * (decimal)rng.NextDouble(), 0),
                Status = rng.Next(0, 2),
            });
        }
        await Flush("ServiceBooking", svcCount);

        // ---- 13) Vé máy bay đoàn: `scale/3` --------------------------------------
        var flightCount = scale / 3;
        for (var i = 1; i <= flightCount; i++)
        {
            var qtyF = rng.Next(10, 40);
            var used = rng.Next(0, qtyF + 1);
            var totalCost = Money(30_000, 200_000);
            await Add(new FlightTicket
            {
                Pnr = $"PNRP{i:D5}",
                MarketRef = PickId(marketIds).ToString(),
                ProviderRef = PickId(providerIds).ToString(),
                TourType = Pick(tourTypes),
                Days = rng.Next(2, 8),
                DepartureDate = SpreadDate(-90, 150),
                Quantity = qtyF,
                UsedQuantity = used,
                OrderRef = rng.Next(0, 2) == 0 ? PickId(orderIds).ToString() : null,
                TotalCost = totalCost,
                PaidAmount = Math.Round(totalCost * (decimal)rng.NextDouble(), 0),
                ReservedAmount = Money(0, 20_000),
                Status = 1,
            });
        }
        await Flush("FlightTicket", flightCount);

        // ---- 14) Vé máy bay lẻ: `scale/3` ----------------------------------------
        var flightIndCount = scale / 3;
        for (var i = 1; i <= flightIndCount; i++)
        {
            var tripType = rng.Next(0, 2);
            var sell = Money(1_500, 6_000);
            var received = Math.Round(sell * (decimal)rng.NextDouble(), 0);
            var tc = Math.Round(sell * 0.85m, 0);
            var depart = SpreadDate(-30, 90);
            await Add(new FlightTicketIndividual
            {
                Code = $"VMBP{i:D5}",
                TicketCode = $"738-{rng.Next(1000000, 9999999)}",
                Pnr = $"PNL{i:D5}",
                CustomerName = Name(),
                ProviderRef = PickId(providerIds).ToString(),
                OrderRef = rng.Next(0, 2) == 0 ? PickId(orderIds).ToString() : null,
                TripType = tripType,
                Route = Pick(routes),
                DepartDate = depart,
                ReturnDate = tripType == 1 ? depart.AddDays(rng.Next(2, 10)) : null,
                SellAmount = sell,
                ReceivedAmount = received,
                TotalCost = tc,
                PaidAmount = Math.Round(tc * (decimal)rng.NextDouble(), 0),
                PaymentDueDate = now.AddDays(rng.Next(-10, 20)),
                Status = rng.Next(0, 3),
                AssigneeRef = PickId(userIds).ToString(),
            });
        }
        await Flush("FlightTicketIndividual", flightIndCount);

        // ---- 15) Công việc nội bộ: `scale/3` -------------------------------------
        var taskCount = scale / 3;
        for (var i = 0; i < taskCount; i++)
        {
            await Add(new WorkTask
            {
                Title = $"Việc perf #{i} — {Pick(["Chuẩn bị hồ sơ", "Đặt vé", "Xác nhận KS", "Tổng kết doanh thu", "Gọi khách"])}",
                Description = "Việc tự sinh cho test hiệu năng.",
                AssigneeUserId = PickId(userIds),
                DueDate = now.AddDays(rng.Next(-15, 30)),
                Priority = rng.Next(0, 3),
                Status = rng.Next(0, 4),
                RelatedOrderId = rng.Next(0, 3) == 0 ? PickId(orderIds) : null,
            });
        }
        await Flush("WorkTask", taskCount);

        // ---- 16) Thông báo: `scale` ----------------------------------------------
        for (var i = 0; i < scale; i++)
        {
            await Add(new Notification
            {
                UserId = PickId(userIds),
                Title = $"Thông báo perf #{i}",
                Message = Pick(["Đơn hàng mới cần xử lý.", "Phiếu thu chờ duyệt.", "Tour khởi hành sắp tới.", "Báo giá đã gửi khách.", "Công nợ cần thu."]),
                LinkUrl = Pick(["/orders", "/receipts", "/quotes", "/operations-calendar"]),
                IsRead = rng.Next(0, 2) == 1,
            });
        }
        await Flush("Notification", scale);

        // ---- 17) Backdate CreatedAt (best-effort, Postgres) để lưới sort đa dạng --
        // AppDbContext ép CreatedAt=now khi INSERT; rải lại bằng SQL. Bọc try/catch để KHÔNG chặn khởi động.
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Customers\" SET \"CreatedAt\" = now() - (abs(hashtext(\"Id\"::text)) % 365) * interval '1 day' WHERE \"Code\" LIKE 'KHP%'");
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Orders\" SET \"CreatedAt\" = now() - (abs(hashtext(\"Id\"::text)) % 365) * interval '1 day' WHERE \"Code\" LIKE 'ODP%'");
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Leads\" SET \"CreatedAt\" = now() - (abs(hashtext(\"Id\"::text)) % 365) * interval '1 day'");
                Console.WriteLine("[PerfSeed] Đã backdate CreatedAt (Customers/Orders/Leads).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PerfSeed] Bỏ qua backdate CreatedAt: {ex.Message}");
            }
        }

        sw.Stop();
        Console.WriteLine($"[PerfSeed] HOÀN TẤT scale={scale} sau {sw.Elapsed.TotalSeconds:F1}s. " +
            "(ActivityLog tự sinh bởi interceptor cho mỗi INSERT.)");
    }
}
