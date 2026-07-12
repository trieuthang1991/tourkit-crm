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

        // 7c) Lịch chăm sóc/hẹn (để dashboard "Quản lý lịch hẹn" có số) — idempotent riêng.
        if (!await db.Set<CustomerCare>().AnyAsync())
        {
            db.AddRange(
                new CustomerCare { CustomerId = c1.Id, Title = "Gọi xác nhận tour Hạ Long", Detail = "Chốt số lượng khách", RemindAt = now.AddDays(1), AssignedToUserId = uSalesHn.Id, Status = 0 },
                new CustomerCare { CustomerId = c2.Id, Title = "Nhắc thanh toán còn lại", Detail = "Nhắc chuyển khoản đợt 2", RemindAt = now.AddDays(2), AssignedToUserId = uSalesHcm.Id, Status = 1 },
                new CustomerCare { CustomerId = c3.Id, Title = "Chăm sóc sau tour", Detail = "Xin feedback + ưu đãi lần sau", RemindAt = now.AddDays(-1), AssignedToUserId = uOps.Id, Status = 2 });
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
