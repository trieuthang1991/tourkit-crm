using Microsoft.EntityFrameworkCore;
using TourKit.Api.Auth;
using TourKit.Api.Provisioning;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Api.DevData;

/// <summary>
/// Seed BỘ DATA MẪU vào DB (Development) để kiểm tra trực quan các thanh lọc:
/// chi nhánh · phòng ban · NV phụ trách · loại tour · NCC · nguồn · trạng thái · thanh toán...
/// Get-or-create theo MÃ tự nhiên (không xoá dữ liệu sẵn có, không nhân bản). Ghi thẳng Postgres —
/// không dùng dữ liệu RAM. Mốc nghiệp vụ: đơn OD_0001 (đã có thì bỏ qua phần đơn/phiếu/lead).
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoSlug = "demo-tour";

    public static async Task SeedAsync(
        AppDbContext db, AmbientTenantContext ambient, IProvisioningService provisioning, IPasswordHasher hasher)
    {
        // 1) Bảo đảm có tenant demo (tự provision đủ tenant + admin + role + quyền + subscription).
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == DemoSlug && !t.IsDeleted);
        if (tenant is null)
        {
            var outcome = await provisioning.RegisterAsync(new RegisterTenantRequest(
                CompanyName: "Demo Tour", Slug: DemoSlug,
                AdminEmail: "admin@demo.vn", AdminPassword: "Demo@12345", AdminFullName: "Quản trị Demo"));
            if (outcome.Error != RegistrationError.None || outcome.Response is null)
            {
                return; // không tạo được thì thôi, không chặn khởi động
            }

            tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == outcome.Response.TenantId);
        }

        // 2) Mọi thao tác đọc/ghi tiếp theo scope theo tenant demo (interceptor tự gán TenantId khi lưu).
        ambient.SetTenant(tenant.Id);

        var now = DateTimeOffset.UtcNow;

        // --- Get-or-create helpers (theo mã/tên tự nhiên) ---
        async Task<Branch> BranchOf(string code, string name, int sort)
        {
            var e = await db.Set<Branch>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null) { e = new Branch { Code = code, Name = name, SortOrder = sort }; db.Add(e); }
            return e;
        }
        async Task<Department> DeptOf(string code, string name, int sort)
        {
            var e = await db.Set<Department>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null) { e = new Department { Code = code, Name = name, SortOrder = sort }; db.Add(e); }
            return e;
        }
        async Task<Position> PosOf(string name, int sort)
        {
            var e = await db.Set<Position>().FirstOrDefaultAsync(x => x.Name == name);
            if (e is null) { e = new Position { Name = name, SortOrder = sort }; db.Add(e); }
            return e;
        }
        async Task<MarketType> MarketOf(string name, int sort)
        {
            var e = await db.Set<MarketType>().FirstOrDefaultAsync(x => x.Name == name);
            if (e is null) { e = new MarketType { Name = name, SortOrder = sort }; db.Add(e); }
            return e;
        }

        // 3) Danh mục ------------------------------------------------------------
        var brHn = await BranchOf("CN-HN", "Chi nhánh Hà Nội", 1);
        var brHcm = await BranchOf("CN-HCM", "Chi nhánh TP.HCM", 2);
        var depKd = await DeptOf("KD", "Phòng Kinh doanh", 1);
        var depDh = await DeptOf("DH", "Phòng Điều hành", 2);
        var posNv = await PosOf("Nhân viên", 1);
        var posTp = await PosOf("Trưởng phòng", 2);
        var mtInbound = await MarketOf("Inbound", 1);
        var mtOutbound = await MarketOf("Outbound", 2);
        var mtDomestic = await MarketOf("Nội địa", 3);
        await db.SaveChangesAsync();
        // Thị trường con (cha-con) — để kiểm tra lọc theo cha bao gồm con.
        async Task<MarketType> ChildMarketOf(string name, Guid parentId, int sort)
        {
            var e = await db.Set<MarketType>().FirstOrDefaultAsync(x => x.Name == name);
            if (e is null) { e = new MarketType { Name = name, ParentId = parentId, SortOrder = sort }; db.Add(e); }
            return e;
        }
        var mtMienBac = await ChildMarketOf("Nội địa - Miền Bắc", mtDomestic.Id, 1);
        var mtMienNam = await ChildMarketOf("Nội địa - Miền Nam", mtDomestic.Id, 2);
        await db.SaveChangesAsync();

        // 4) Người dùng (NV phụ trách/tạo) — get-or-create theo email --------------
        async Task<User> UserOf(string email, string fullName, Guid deptId, Guid posId)
        {
            var e = await db.Set<User>().FirstOrDefaultAsync(x => x.Email == email);
            if (e is null)
            {
                e = new User { Email = email, FullName = fullName, PasswordHash = hasher.Hash("Demo@12345"), DepartmentId = deptId, PositionId = posId };
                db.Add(e);
            }
            return e;
        }
        var uSalesHn = await UserOf("sales.hn@demo.vn", "Trần Kinh Doanh (HN)", depKd.Id, posNv.Id);
        var uSalesHcm = await UserOf("sales.hcm@demo.vn", "Lê Kinh Doanh (HCM)", depKd.Id, posNv.Id);
        var uOps = await UserOf("ops.hn@demo.vn", "Phạm Điều Hành", depDh.Id, posTp.Id);
        await db.SaveChangesAsync();

        // 5) Khách hàng — get-or-create theo mã ----------------------------------
        async Task<Customer> CustOf(string code, string name, string phone, int type, string source, string? tag, string address)
        {
            var e = await db.Set<Customer>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null)
            {
                e = new Customer { Code = code, FullName = name, Phone = phone, CustomerType = type, Source = source, Tag = tag, Address = address };
                db.Add(e);
            }
            return e;
        }
        var c1 = await CustOf("KH_00001", "Nguyễn Văn An", "0901000001", 0, "Facebook", "VIP", "Hà Nội");
        var c2 = await CustOf("KH_00002", "Trần Thị Bình", "0901000002", 0, "Website", "Thân thiết", "Hồ Chí Minh");
        var c3 = await CustOf("KH_00003", "Công ty Du Lịch Xanh", "0901000003", 1, "Giới thiệu", null, "Đà Nẵng");
        var c4 = await CustOf("KH_00004", "Lê Hoàng Cường", "0901000004", 3, "Facebook", null, "Hải Phòng");
        var c5 = await CustOf("KH_00005", "Phạm Thu Dung", "0901000005", 0, "Website", null, "Hà Nội");

        // 6) Nhà cung cấp — get-or-create theo mã --------------------------------
        async Task<Provider> ProvOf(string code, string name, ProviderType type, string province, Guid branchId, Guid marketId, int rate)
        {
            var e = await db.Set<Provider>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null)
            {
                e = new Provider { Code = code, Name = name, Type = type, Province = province, BranchId = branchId, MarketTypeId = marketId, Rate = rate };
                db.Add(e);
            }
            return e;
        }
        var p1 = await ProvOf("NCC_KS01", "Khách sạn Mường Thanh", ProviderType.Hotel, "Hà Nội", brHn.Id, mtDomestic.Id, 4);
        var p2 = await ProvOf("NCC_XE01", "Vận tải Thành Bưởi", ProviderType.Vehicle, "Hồ Chí Minh", brHcm.Id, mtDomestic.Id, 4);
        var p3 = await ProvOf("NCC_NH01", "Nhà hàng Sen Hồ Tây", ProviderType.Restaurant, "Hà Nội", brHn.Id, mtInbound.Id, 5);
        var p4 = await ProvOf("NCC_HDV1", "HDV Nguyễn Minh", ProviderType.Guide, "Đà Nẵng", brHcm.Id, mtInbound.Id, 5);
        var p5 = await ProvOf("NCC_HK01", "Vietnam Airlines", ProviderType.Airline, "Hà Nội", brHn.Id, mtOutbound.Id, 5);
        await db.SaveChangesAsync();

        // 7) Mẫu tour + chuyến khởi hành (loại tour inbound/outbound/domestic) ----
        async Task<TourTemplate> TplOf(string code, string title, string type, decimal adult, decimal child)
        {
            var e = await db.Set<TourTemplate>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null) { e = new TourTemplate { Code = code, Title = title, TourType = type, PriceAdult = adult, PriceChild = child, ReservationHours = 48 }; db.Add(e); }
            return e;
        }
        var tplHalong = await TplOf("TPL_HALONG", "Hạ Long 3N2Đ", "domestic", 3_500_000m, 2_500_000m);
        var tplThai = await TplOf("TPL_THAILAN", "Thái Lan 5N4Đ", "outbound", 8_900_000m, 6_500_000m);
        await db.SaveChangesAsync();

        async Task<TourDeparture> DepOf(string code, string title, string type, int daysToGo, int slots, Guid? parentId)
        {
            var e = await db.Set<TourDeparture>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null)
            {
                e = new TourDeparture { Code = code, Title = title, TourType = type, DepartureDate = now.AddDays(daysToGo), EndDate = now.AddDays(daysToGo + 2), TotalSlots = slots, ParentTourId = parentId, AssignedToUserId = uOps.Id };
                db.Add(e);
            }
            return e;
        }
        var depHalong = await DepOf("DEP_HL_01", "Hạ Long 3N2Đ — chuyến 1", "domestic", 20, 40, tplHalong.Id);
        var depThai = await DepOf("DEP_TL_01", "Thái Lan 5N4Đ — chuyến 1", "outbound", 35, 30, tplThai.Id);
        var depInbound = await DepOf("DEP_IN_01", "Hanoi City Tour (Inbound)", "inbound", 15, 20, null);
        await db.SaveChangesAsync();

        // 7b) Tỉ lệ hoa hồng theo NV (để dashboard "Tiền hoa hồng" có số) — idempotent riêng.
        if (!await db.Set<CommissionRule>().AnyAsync())
        {
            db.AddRange(
                new CommissionRule { UserId = uSalesHn.Id, Percentage = 5m, Status = 1 },
                new CommissionRule { UserId = uSalesHcm.Id, Percentage = 8m, Status = 1 });
            await db.SaveChangesAsync();
        }

        // 7b') Danh mục loại khách (Code khớp Customer.CustomerType) — cho màn Danh mục + HH theo loại khách.
        if (!await db.Set<CustomerType>().AnyAsync())
        {
            db.AddRange(
                new CustomerType { Code = 0, Name = "Khách lẻ", SortOrder = 1, Status = 1 },
                new CustomerType { Code = 1, Name = "Doanh nghiệp", SortOrder = 2, Status = 1 },
                new CustomerType { Code = 2, Name = "Đại lý", SortOrder = 3, Status = 1 },
                new CustomerType { Code = 3, Name = "Cộng tác viên", SortOrder = 4, Status = 1 });
            await db.SaveChangesAsync();
        }

        // 7b'') Hoa hồng theo loại khách — idempotent riêng.
        if (!await db.Set<CustomerCommissionRule>().AnyAsync())
        {
            db.AddRange(
                new CustomerCommissionRule { CustomerType = 1, Percentage = 3m, Status = 1 },
                new CustomerCommissionRule { CustomerType = 2, Percentage = 7m, Status = 1 },
                new CustomerCommissionRule { CustomerType = 3, Percentage = 10m, Status = 1 });
            await db.SaveChangesAsync();
        }

        // 7b''') Danh mục dùng chung (Thiết lập hệ thống) — mỗi catalog idempotent riêng, Status=1 Hoạt động.
        if (!await db.Set<CarType>().AnyAsync())
        {
            db.AddRange(
                new CarType { Code = 4, Name = "4 chỗ", SortOrder = 1, Status = 1 },
                new CarType { Code = 7, Name = "7 chỗ", SortOrder = 2, Status = 1 },
                new CarType { Code = 16, Name = "16 chỗ", SortOrder = 3, Status = 1 },
                new CarType { Code = 29, Name = "29 chỗ", SortOrder = 4, Status = 1 },
                new CarType { Code = 35, Name = "35 chỗ", SortOrder = 5, Status = 1 },
                new CarType { Code = 45, Name = "45 chỗ", SortOrder = 6, Status = 1 },
                new CarType { Code = 47, Name = "47 chỗ", SortOrder = 7, Status = 1 });
        }
        if (!await db.Set<LanguageType>().AnyAsync())
        {
            // Bám hệ cũ "Ngôn ngữ HDV": Anh/Việt/Hàn/Nhật/Trung.
            db.AddRange(
                new LanguageType { Name = "Tiếng Anh", Code = "en", SortOrder = 1, Status = 1 },
                new LanguageType { Name = "Tiếng Việt", Code = "vi", SortOrder = 2, Status = 1 },
                new LanguageType { Name = "Tiếng Hàn", Code = "ko", SortOrder = 3, Status = 1 },
                new LanguageType { Name = "Tiếng Nhật", Code = "ja", SortOrder = 4, Status = 1 },
                new LanguageType { Name = "Tiếng Trung", Code = "zh", SortOrder = 5, Status = 1 });
        }
        if (!await db.Set<RoomClass>().AnyAsync())
        {
            // Bám hệ cũ "Class Hotel" (legacy class_hotel): hạng/loại cơ sở lưu trú, KHÔNG phải sao.
            db.AddRange(
                new RoomClass { Name = "Khu nghỉ dưỡng - Resort", SortOrder = 1, Status = 1 },
                new RoomClass { Name = "Khách sạn - Hotel", SortOrder = 2, Status = 1 },
                new RoomClass { Name = "Căn hộ - Apartment", SortOrder = 3, Status = 1 },
                new RoomClass { Name = "Villa", SortOrder = 4, Status = 1 },
                new RoomClass { Name = "Nhà nghỉ - Motel", SortOrder = 5, Status = 1 });
        }
        if (!await db.Set<CustomerSource>().AnyAsync())
        {
            db.AddRange(
                new CustomerSource { Name = "Facebook", SortOrder = 1, Status = 1 },
                new CustomerSource { Name = "Website", SortOrder = 2, Status = 1 },
                new CustomerSource { Name = "Giới thiệu", SortOrder = 3, Status = 1 },
                new CustomerSource { Name = "Zalo", SortOrder = 4, Status = 1 });
        }
        if (!await db.Set<CustomerTag>().AnyAsync())
        {
            db.AddRange(
                new CustomerTag { Name = "VIP", Color = "red", SortOrder = 1, Status = 1 },
                new CustomerTag { Name = "Thân thiết", Color = "green", SortOrder = 2, Status = 1 },
                new CustomerTag { Name = "Tiềm năng", Color = "blue", SortOrder = 3, Status = 1 });
        }
        if (!await db.Set<Currency>().AnyAsync())
        {
            db.AddRange(
                new Currency { Code = "VND", Name = "Việt Nam Đồng", RateToVnd = 1m, SortOrder = 1, Status = 1 },
                new Currency { Code = "USD", Name = "Đô la Mỹ", RateToVnd = 25_000m, SortOrder = 2, Status = 1 },
                new Currency { Code = "EUR", Name = "Euro", RateToVnd = 27_000m, SortOrder = 3, Status = 1 });
        }
        if (!await db.Set<Surcharge>().AnyAsync())
        {
            db.AddRange(
                new Surcharge { Name = "Phụ thu phòng đơn", CalcType = 0, DefaultValue = 500_000m, SortOrder = 1, Status = 1 },
                new Surcharge { Name = "Phụ thu cao điểm", CalcType = 1, DefaultValue = 10m, SortOrder = 2, Status = 1 });
        }
        if (!await db.Set<PaymentTerm>().AnyAsync())
        {
            db.AddRange(
                new PaymentTerm { Name = "Thanh toán ngay", Description = "Thanh toán 100% khi đặt", SortOrder = 1, Status = 1 },
                new PaymentTerm { Name = "Cọc 50%", Description = "Cọc 50%, còn lại trước khởi hành", SortOrder = 2, Status = 1 },
                new PaymentTerm { Name = "Công nợ 30 ngày", Description = "Thanh toán trong 30 ngày", SortOrder = 3, Status = 1 });
        }
        if (!await db.Set<PaymentAccount>().AnyAsync())
        {
            db.Add(new PaymentAccount { Name = "VCB - Công ty Demo Tour", BankName = "Vietcombank", AccountNumber = "0011000123456", AccountHolder = "CONG TY DEMO TOUR", Branch = "Hà Nội", TransferNote = "Thanh toan tour", IsDefault = true, SortOrder = 1, Status = 1 });
        }
        await db.SaveChangesAsync();

        // 7c) Lịch chăm sóc/hẹn (để dashboard "Quản lý lịch hẹn" có số) — idempotent riêng.
        if (!await db.Set<CustomerCare>().AnyAsync())
        {
            db.AddRange(
                new CustomerCare { CustomerId = c1.Id, Title = "Gọi xác nhận tour Hạ Long", Detail = "Chốt số lượng khách", RemindAt = now.AddDays(1), AssignedToUserId = uSalesHn.Id, Status = 0 },
                new CustomerCare { CustomerId = c2.Id, Title = "Nhắc thanh toán còn lại", Detail = "Nhắc chuyển khoản đợt 2", RemindAt = now.AddDays(2), AssignedToUserId = uSalesHcm.Id, Status = 1 },
                new CustomerCare { CustomerId = c3.Id, Title = "Chăm sóc sau tour", Detail = "Xin feedback + ưu đãi lần sau", RemindAt = now.AddDays(-1), AssignedToUserId = uOps.Id, Status = 2 });
            await db.SaveChangesAsync();
        }

        // 7c') Công việc nội bộ (Tasking) — idempotent riêng; đủ trạng thái/ưu tiên + 1 quá hạn.
        if (!await db.Set<WorkTask>().AnyAsync())
        {
            db.AddRange(
                new WorkTask { Title = "Chuẩn bị hồ sơ đoàn Hạ Long", Description = "Danh sách khách + hợp đồng", AssigneeUserId = uSalesHn.Id, DueDate = now.AddDays(3), Priority = 2, Status = 0 },
                new WorkTask { Title = "Đặt vé máy bay Thái Lan", Description = "Liên hệ Vietnam Airlines", AssigneeUserId = uOps.Id, DueDate = now.AddDays(5), Priority = 1, Status = 1 },
                new WorkTask { Title = "Xác nhận khách sạn Đà Nẵng", Description = "Mường Thanh 3N2Đ", AssigneeUserId = uSalesHcm.Id, DueDate = now.AddDays(-2), Priority = 2, Status = 1 },
                new WorkTask { Title = "Tổng kết doanh thu tháng", Description = "Báo cáo cho CEO", AssigneeUserId = uOps.Id, DueDate = now.AddDays(-5), Priority = 1, Status = 2 },
                new WorkTask { Title = "Huỷ đặt xe dư", Description = "Đoàn giảm khách", AssigneeUserId = uSalesHn.Id, DueDate = now.AddDays(1), Priority = 0, Status = 3 });
            await db.SaveChangesAsync();
        }

        // 7c'') Chiến dịch marketing + log gửi — idempotent riêng (1 nháp, 2 đã gửi).
        if (!await db.Set<MarketingCampaign>().AnyAsync())
        {
            var cmpHe = new MarketingCampaign { Name = "Ưu đãi Hè 2026", Channel = MarketingChannel.Email, Subject = "Giảm 20% tour hè", Body = "Đặt tour hè giảm ngay 20%.", Status = 1 };
            var cmpZalo = new MarketingCampaign { Name = "Chăm sóc khách VIP", Channel = MarketingChannel.Zalo, Subject = null, Body = "Cảm ơn quý khách đã đồng hành.", Status = 1 };
            var cmpSms = new MarketingCampaign { Name = "Nhắc lịch khởi hành", Channel = MarketingChannel.Sms, Subject = null, Body = "Tour của bạn khởi hành trong 3 ngày.", Status = 0 };
            db.AddRange(cmpHe, cmpZalo, cmpSms);
            await db.SaveChangesAsync();

            db.AddRange(
                new MarketingSendLog { CampaignId = cmpHe.Id, Recipient = "an.nguyen@gmail.com", Status = 1, SentAt = now.AddDays(-3) },
                new MarketingSendLog { CampaignId = cmpHe.Id, Recipient = "binh.tran@gmail.com", Status = 1, SentAt = now.AddDays(-3) },
                new MarketingSendLog { CampaignId = cmpZalo.Id, Recipient = "0901000004", Status = 1, SentAt = now.AddDays(-1) });
            await db.SaveChangesAsync();
        }

        // 7d) Nhóm tour (Nhóm) — idempotent; dùng cho filter lưới vận hành.
        async Task<TourGroup> GroupOf(string code, string name, int sort)
        {
            var e = await db.Set<TourGroup>().FirstOrDefaultAsync(x => x.Code == code);
            if (e is null) { e = new TourGroup { Code = code, Name = name, SortOrder = sort }; db.Add(e); }
            return e;
        }
        var gInbound = await GroupOf("NHOM-IN", "Nhóm Inbound", 1);
        var gDomestic = await GroupOf("NHOM-ND", "Nhóm Nội địa", 2);
        await db.SaveChangesAsync();

        // 7e) Backfill Thị trường/Nhóm/Loại tour lên đơn demo (đơn đã tạo từ trước) nếu chưa có.
        var demoOrders = await db.Set<Order>().Where(o => o.Code.StartsWith("OD_")).ToListAsync();
        if (demoOrders.Count > 0 && demoOrders.All(o => o.MarketTypeId == null))
        {
            // Gán xen kẽ để mỗi filter có ≥1 kết quả: loại tour FIT/GIT, thị trường inbound/domestic, nhóm tương ứng.
            for (var i = 0; i < demoOrders.Count; i++)
            {
                var o = demoOrders[i];
                var inbound = i % 2 == 0;
                o.MarketTypeId = inbound ? mtInbound.Id : mtDomestic.Id;
                o.TourGroupId = inbound ? gInbound.Id : gDomestic.Id;
                o.BookingType = i % 3;   // 0 FIT · 1 GIT · 2 LandTour/Combo
                o.IsCommissionSettled = i % 2 == 1;
                db.Update(o);
            }
            await db.SaveChangesAsync();
        }

        // 7f) Chuyển vài đơn Nội địa sang thị trường CON (để kiểm tra lọc cha bao gồm con) — idempotent.
        if (!await db.Set<Order>().AnyAsync(o => o.MarketTypeId == mtMienBac.Id))
        {
            var domesticOrders = await db.Set<Order>().Where(o => o.MarketTypeId == mtDomestic.Id).OrderBy(o => o.Code).ToListAsync();
            if (domesticOrders.Count > 0)
            {
                domesticOrders[0].MarketTypeId = mtMienBac.Id;
                db.Update(domesticOrders[0]);
                if (domesticOrders.Count > 1)
                {
                    domesticOrders[1].MarketTypeId = mtMienNam.Id;
                    db.Update(domesticOrders[1]);
                }
                await db.SaveChangesAsync();
            }
        }

        // 7g) Tình trạng vận hành + CTV lên đơn demo — idempotent (mặc định tất cả Upcoming).
        var opsOrders = await db.Set<Order>().Where(o => o.Code.StartsWith("OD_")).OrderBy(o => o.Code).ToListAsync();
        // (int)0 = giá trị mặc định DB cho cột mới trên row cũ; <=1 nghĩa là chưa gán tình trạng thật.
        if (opsOrders.Count > 0 && opsOrders.All(o => (int)o.OperationalStatus <= 1))
        {
            var vals = new[]
            {
                OrderOperationalStatus.Running, OrderOperationalStatus.Upcoming, OrderOperationalStatus.PendingSettlement,
                OrderOperationalStatus.Done, OrderOperationalStatus.Cancelled,
            };
            for (var i = 0; i < opsOrders.Count; i++)
            {
                opsOrders[i].OperationalStatus = vals[i % vals.Length];
                opsOrders[i].CollaboratorId = i % 2 == 0 ? c4.Id : null; // c4 = KH loại CTV (CustomerType 3)
                db.Update(opsOrders[i]);
            }
            await db.SaveChangesAsync();
        }

        // 7h) Hoá đơn VAT (varied trạng thái) cho màn Hoá đơn + filter TT hóa đơn — idempotent theo số HĐ.
        if (!await db.Set<Invoice>().AnyAsync(i => i.Number == "0000001"))
        {
            db.Add(new Invoice
            {
                OrderId = opsOrders.FirstOrDefault()?.Id, Series = "1C26TK", Number = "0000001", InvoiceDate = now,
                BuyerName = "Nguyễn Văn An", BuyerTaxCode = "0101234567", Subtotal = 6_363_636m, VatAmount = 636_364m, TotalAmount = 7_000_000m, Status = 1,
            });
            await db.SaveChangesAsync();
        }
        if (!await db.Set<Invoice>().AnyAsync(i => i.Number == "0000002"))
        {
            db.Add(new Invoice
            {
                Series = "1C26TK", Number = "0000002", InvoiceDate = now.AddDays(-1),
                BuyerName = "Công ty Du Lịch Xanh", BuyerTaxCode = "0209876543", Subtotal = 16_181_818m, VatAmount = 1_618_182m, TotalAmount = 17_800_000m, Status = 0,
            });
            db.Add(new Invoice
            {
                Series = "1C26TK", Number = "0000003", InvoiceDate = now.AddDays(-3),
                BuyerName = "Trần Thị Bình", Subtotal = 5_454_545m, VatAmount = 545_455m, TotalAmount = 6_000_000m, Status = 2,
            });
            await db.SaveChangesAsync();
        }

        // 7i) Chỗ (TourCustomer) cho đơn demo — mỗi đơn 1 dòng, trạng thái xen kẽ để cột "Khách (chỗ)" có số.
        if (!await db.Set<TourCustomer>().AnyAsync())
        {
            foreach (var (o, i) in opsOrders.Select((o, i) => (o, i)))
            {
                var seat = new TourCustomer
                {
                    OrderId = o.Id, TourDepartureId = o.TourDepartureId, CustomerId = o.CustomerId,
                    Quantity = 2, PriceAdult = 1_000_000m, IsMainContact = true, Status = 0,
                };
                switch (i % 4)
                {
                    case 0: seat.HoldExpiresAt = now.AddDays(3); seat.UpfrontAmount = 0m; break;        // Giữ chỗ
                    case 1: seat.UpfrontAmount = 2_000_000m; break;                                     // Đã bán (thanh toán)
                    case 2: seat.UpfrontAmount = 500_000m; break;                                       // Đã bán (đã cọc)
                    default: seat.UpfrontAmount = 0m; break;                                            // Còn lại (chốt chưa bán)
                }
                db.Add(seat);
            }
            await db.SaveChangesAsync();
        }

        // 7j) Báo giá (Quote) — varied trạng thái/số khách/lợi nhuận cho màn Báo giá + phễu dashboard.
        if (!await db.Set<Quote>().AnyAsync())
        {
            db.AddRange(
                new Quote { Code = "BG_0001", CustomerId = c1.Id, CustomerName = c1.FullName, Title = "Hạ Long 3N2Đ", ValidUntil = now.AddDays(10), Status = 2, TotalAmount = 7_000_000m, TotalCost = 4_500_000m, TotalProfit = 2_500_000m, Adults = 2, Children = 1, Infants = 0 },
                new Quote { Code = "BG_0002", CustomerId = c2.Id, CustomerName = c2.FullName, Title = "Thái Lan 5N4Đ", ValidUntil = now.AddDays(15), Status = 1, TotalAmount = 17_800_000m, TotalCost = 11_000_000m, TotalProfit = 6_800_000m, Adults = 2, Children = 0, Infants = 0 },
                new Quote { Code = "BG_0003", CustomerId = c3.Id, CustomerName = c3.FullName, Title = "Đà Nẵng - Hội An", ValidUntil = now.AddDays(5), Status = 0, TotalAmount = 5_400_000m, TotalCost = 3_800_000m, TotalProfit = 1_600_000m, Adults = 3, Children = 2, Infants = 1 },
                new Quote { Code = "BG_0004", CustomerId = c4.Id, CustomerName = c4.FullName, Title = "Sapa 2N1Đ", ValidUntil = now.AddDays(-2), Status = 3, TotalAmount = 3_200_000m, TotalCost = 2_500_000m, TotalProfit = 700_000m, Adults = 1, Children = 0, Infants = 0 });
            await db.SaveChangesAsync();
        }

        // 7k) Booking dịch vụ lẻ (ServiceBooking) — varied loại/NCC cho màn Booking & Dịch vụ.
        if (!await db.Set<ServiceBooking>().AnyAsync())
        {
            var svcOrder = opsOrders.FirstOrDefault();
            ServiceBooking Svc(string code, ServiceBookingType type, Guid? providerId, string desc, int daysToGo, int qty, decimal unit) => new()
            {
                Code = code, Type = type, OrderId = svcOrder?.Id, ProviderId = providerId, Description = desc,
                StartDate = now.AddDays(daysToGo), EndDate = now.AddDays(daysToGo + 2), Quantity = qty, UnitPrice = unit,
                TotalAmount = qty * unit, Status = 0,
            };
            db.AddRange(
                Svc("DV_KS01", ServiceBookingType.Hotel, p1.Id, "Khách sạn Mường Thanh - 2 phòng", 20, 2, 1_200_000m),
                Svc("DV_VE01", ServiceBookingType.Flight, p5.Id, "Vé máy bay HAN-SGN khứ hồi", 20, 4, 1_800_000m),
                Svc("DV_XE01", ServiceBookingType.Transfer, p2.Id, "Xe đưa đón sân bay", 20, 1, 800_000m),
                Svc("DV_VS01", ServiceBookingType.Visa, null, "Visa Thái Lan", 15, 2, 900_000m));
            await db.SaveChangesAsync();
        }

        // 7l) Phân công HDV (TourGuideAssignment) — p4 là HDV (Provider type Guide).
        if (!await db.Set<TourGuideAssignment>().AnyAsync())
        {
            db.AddRange(
                new TourGuideAssignment { TourDepartureId = depHalong.Id, ProviderId = p4.Id, TimeGo = now.AddDays(20), TimeCome = now.AddDays(22), Status = 2 },
                new TourGuideAssignment { TourDepartureId = depThai.Id, ProviderId = p4.Id, TimeGo = now.AddDays(35), TimeCome = now.AddDays(39), Status = 1 });
            await db.SaveChangesAsync();
        }

        // 7m) Xe + phân xe (Vehicle / VehicleAssignment).
        if (!await db.Set<Vehicle>().AnyAsync())
        {
            var xe16 = new Vehicle { Name = "Xe Ford Transit", SeatType = 16, Status = 1 };
            var xe45 = new Vehicle { Name = "Xe Thaco 45 chỗ", SeatType = 45, Status = 1 };
            db.AddRange(xe16, xe45);
            await db.SaveChangesAsync();
            db.AddRange(
                new VehicleAssignment { TourDepartureId = depHalong.Id, VehicleId = xe45.Id, DriverName = "Nguyễn Văn Tài", DriverPhone = "0905111222", TimeGo = now.AddDays(20), TimeCome = now.AddDays(22), Status = 2 },
                new VehicleAssignment { TourDepartureId = depThai.Id, VehicleId = xe16.Id, DriverName = "Trần Văn Lái", DriverPhone = "0905333444", TimeGo = now.AddDays(35), TimeCome = now.AddDays(39), Status = 1 });
            await db.SaveChangesAsync();
        }

        // 7n) Quỹ vé ứng (TicketFund) — vé máy bay do p5 (airline) cấp cho đơn demo.
        if (!await db.Set<TicketFund>().AnyAsync())
        {
            var tfOrder = opsOrders.FirstOrDefault();
            if (tfOrder is not null)
            {
                db.AddRange(
                    new TicketFund { OrderId = tfOrder.Id, ProviderId = p5.Id, TicketCode = "VMB-000001", Status = 1, IsClosed = false },
                    new TicketFund { OrderId = tfOrder.Id, ProviderId = p5.Id, TicketCode = "VMB-000002", Status = 1, IsClosed = true });
                await db.SaveChangesAsync();
            }
        }

        // 7n') Vé máy bay đoàn (FlightTicket) — quỹ vé theo PNR, 1 vé đã gán tour, có hành trình.
        if (!await db.Set<FlightTicket>().AnyAsync())
        {
            static string Itin(params (string d, string f, string fr, string to, string t)[] legs)
                => System.Text.Json.JsonSerializer.Serialize(new { segments = legs.Select(l => new { date = l.d, flightNo = l.f, from = l.fr, to = l.to, depTime = l.t }) });
            var fOrder = opsOrders.FirstOrDefault();
            db.AddRange(
                new FlightTicket
                {
                    Pnr = "PNR0001", MarketRef = mtOutbound.Id.ToString(), ProviderRef = p5.Id.ToString(),
                    TourType = "outbound", Days = 5, DepartureDate = now.AddDays(20), Quantity = 30, UsedQuantity = 10,
                    TotalCost = 180_000_000m, PaidAmount = 100_000_000m, ReservedAmount = 20_000_000m, Status = 1,
                    OrderRef = fOrder?.Id.ToString(),
                    ItineraryJson = Itin(("12/12/2026", "VJ811", "SGN", "SIN", "09:00"), ("17/12/2026", "VJ826", "SIN", "SGN", "13:10")),
                },
                new FlightTicket
                {
                    Pnr = "PNR0002", MarketRef = mtOutbound.Id.ToString(), ProviderRef = p5.Id.ToString(),
                    TourType = "outbound", Days = 4, DepartureDate = now.AddDays(35), Quantity = 20, UsedQuantity = 0,
                    TotalCost = 120_000_000m, PaidAmount = 0m, ReservedAmount = 0m, Status = 1,
                    ItineraryJson = Itin(("05/01/2027", "VN601", "HAN", "BKK", "08:30"), ("09/01/2027", "VN602", "BKK", "HAN", "12:00")),
                },
                new FlightTicket
                {
                    Pnr = "PNR0003", MarketRef = mtInbound.Id.ToString(), ProviderRef = p5.Id.ToString(),
                    TourType = "inbound", Days = 3, DepartureDate = now.AddDays(15), Quantity = 15, UsedQuantity = 15,
                    TotalCost = 45_000_000m, PaidAmount = 45_000_000m, ReservedAmount = 0m, Status = 1,
                    ItineraryJson = Itin(("20/12/2026", "QH201", "HAN", "DAD", "07:00")),
                });
            await db.SaveChangesAsync();
        }

        // 7o) Đại lý B2B (Agent) — varied trạng thái/hạn mức cho màn Đại lý.
        if (!await db.Set<Agent>().AnyAsync())
        {
            db.AddRange(
                new Agent { Code = "DL001", Name = "Đại lý Miền Bắc", ContactPerson = "Nguyễn Văn Bắc", Phone = "0901234001", Email = "mienbac@dl.vn", TaxCode = "0301111111", Address = "Hà Nội", CreditLimit = 100_000_000m, Status = 1 },
                new Agent { Code = "DL002", Name = "Đại lý Miền Nam", ContactPerson = "Trần Thị Nam", Phone = "0901234002", Email = "miennam@dl.vn", TaxCode = "0302222222", Address = "Hồ Chí Minh", CreditLimit = 150_000_000m, Status = 1 },
                new Agent { Code = "DL003", Name = "Đại lý Miền Trung", ContactPerson = "Lê Văn Trung", Phone = "0901234003", Address = "Đà Nẵng", CreditLimit = 50_000_000m, Status = 0 });
            await db.SaveChangesAsync();
        }

        // 7p) Đặt chỗ đại lý (AgentBooking) — từ 1 yêu cầu báo giá đã xác nhận.
        if (!await db.Set<AgentBooking>().AnyAsync())
        {
            var agent = await db.Set<Agent>().FirstOrDefaultAsync(a => a.Code == "DL001");
            if (agent is not null)
            {
                var qr = new AgentQuoteRequest { AgentId = agent.Id, ProductName = "Tour Đà Nẵng 3N2Đ (B2B)", PaxCount = 10, Status = AgentQuoteStatus.Confirmed, QuotedAmount = 25_000_000m };
                db.Add(qr);
                await db.SaveChangesAsync();
                db.AddRange(
                    new AgentBooking { AgentId = agent.Id, QuoteRequestId = qr.Id, Code = "ABK-0001", TotalAmount = 25_000_000m, Status = 1 },
                    new AgentBooking { AgentId = agent.Id, QuoteRequestId = qr.Id, Code = "ABK-0002", TotalAmount = 12_000_000m, Status = 0 });
                await db.SaveChangesAsync();
            }
        }

        // 8) Đơn/chi phí/phiếu/lead — chỉ seed khi CHƯA có đơn mốc OD_0001 --------
        if (await db.Set<Order>().AnyAsync(o => o.Code == "OD_0001"))
        {
            return;
        }

        Order MkOrder(string code, Guid depId, Guid custId, Guid branchId, Guid salesId, Guid createdBy, OrderStatus status, decimal revenue)
            => new() { Code = code, TourDepartureId = depId, CustomerId = custId, BranchId = branchId, SalesUserId = salesId, CreatedByUserId = createdBy, Status = status, TotalRevenue = revenue };

        var o1 = MkOrder("OD_0001", depHalong.Id, c1.Id, brHn.Id, uSalesHn.Id, uSalesHn.Id, OrderStatus.Confirmed, 7_000_000m);
        var o2 = MkOrder("OD_0002", depThai.Id, c2.Id, brHcm.Id, uSalesHcm.Id, uSalesHcm.Id, OrderStatus.Confirmed, 17_800_000m);
        var o3 = MkOrder("OD_0003", depInbound.Id, c3.Id, brHn.Id, uSalesHn.Id, uOps.Id, OrderStatus.Draft, 2_400_000m);
        var o4 = MkOrder("OD_0004", depHalong.Id, c4.Id, brHcm.Id, uSalesHcm.Id, uSalesHcm.Id, OrderStatus.Confirmed, 6_000_000m);
        var o5 = MkOrder("OD_0005", depThai.Id, c5.Id, brHn.Id, uSalesHn.Id, uSalesHn.Id, OrderStatus.Cancelled, 8_900_000m);
        db.AddRange(o1, o2, o3, o4, o5);
        await db.SaveChangesAsync();

        // 9) Dòng chi phí (NCC theo đơn) -----------------------------------------
        OrderCost MkCost(Guid orderId, Guid providerId, string svc, decimal amount)
            => new() { OrderId = orderId, ProviderId = providerId, ServiceName = svc, DayIndex = 1, ExpectedAmount = amount, ActualAmount = amount, Status = 1 };
        db.AddRange(
            MkCost(o1.Id, p1.Id, "Phòng khách sạn", 3_000_000m),
            MkCost(o1.Id, p2.Id, "Xe đưa đón", 1_500_000m),
            MkCost(o2.Id, p5.Id, "Vé máy bay", 9_000_000m),
            MkCost(o2.Id, p4.Id, "Hướng dẫn viên", 2_000_000m),
            MkCost(o3.Id, p3.Id, "Ăn trưa", 800_000m),
            MkCost(o4.Id, p1.Id, "Phòng khách sạn", 2_500_000m));

        // 10) Phiếu thu (varied trạng thái/hình thức/số tiền/ngày; ghi nhận để tính đã thu) --
        ReceiptVoucher MkReceipt(string code, Guid orderId, decimal amount, string method, int status, int daysAgo, string? payer)
            => new() { Code = code, OrderId = orderId, Amount = amount, PaymentMethod = method, Status = status, IsRecognized = status == 1, IssuedAt = now.AddDays(-daysAgo), Partner = payer };
        db.AddRange(
            MkReceipt("PT_0001", o1.Id, 3_000_000m, "cash", 1, 10, "Nguyễn Văn An"),   // đã cọc (một phần)
            MkReceipt("PT_0002", o2.Id, 17_800_000m, "bank", 1, 8, "Trần Thị Bình"),   // thanh toán hết
            MkReceipt("PT_0003", o4.Id, 2_000_000m, "cash", 0, 3, "Lê Hoàng Cường"),   // chờ duyệt
            MkReceipt("PT_0004", o3.Id, 1_000_000m, "bank", 2, 1, "Cty Du Lịch Xanh")); // từ chối

        // 11) Phiếu chi (trả NCC) ------------------------------------------------
        PaymentVoucher MkPayment(string code, Guid orderId, Guid providerId, decimal amount, string method, int status, int daysAgo)
            => new() { Code = code, OrderId = orderId, ProviderId = providerId, Amount = amount, PaymentMethod = method, Status = status, IsRecognized = status == 1, IssuedAt = now.AddDays(-daysAgo) };
        db.AddRange(
            MkPayment("PC_0001", o1.Id, p1.Id, 3_000_000m, "bank", 1, 9),
            MkPayment("PC_0002", o2.Id, p5.Id, 9_000_000m, "bank", 1, 7),
            MkPayment("PC_0003", o1.Id, p2.Id, 1_500_000m, "cash", 0, 2));

        // 12) Cơ hội bán hàng (Lead) — varied trạng thái/nguồn/chi nhánh/phụ trách --
        Lead MkLead(string name, string phone, string source, LeadStatus status, Guid branchId, Guid assignedTo, Guid createdBy)
            => new() { FullName = name, Phone = phone, Source = source, Status = status, BranchId = branchId, AssignedToUserId = assignedTo, CreatedByUserId = createdBy };
        db.AddRange(
            MkLead("Vũ Minh Khôi", "0912000001", "Facebook", LeadStatus.New, brHn.Id, uSalesHn.Id, uSalesHn.Id),
            MkLead("Đỗ Thanh Hà", "0912000002", "Website", LeadStatus.Contacted, brHcm.Id, uSalesHcm.Id, uSalesHcm.Id),
            MkLead("Bùi Anh Tú", "0912000003", "Giới thiệu", LeadStatus.Qualified, brHn.Id, uSalesHn.Id, uOps.Id),
            MkLead("Ngô Bảo Ngọc", "0912000004", "Facebook", LeadStatus.Won, brHcm.Id, uSalesHcm.Id, uSalesHcm.Id),
            MkLead("Cao Đức Duy", "0912000005", "Website", LeadStatus.Lost, brHn.Id, uSalesHn.Id, uSalesHn.Id));

        await db.SaveChangesAsync();
    }
}
