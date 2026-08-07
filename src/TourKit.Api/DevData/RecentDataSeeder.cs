using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;
using TourKit.Shared.Entities;

namespace TourKit.Api.DevData;

/// <summary>
/// DEV-ONLY: bơm dữ liệu mẫu NEO VÀO HÔM NAY để các màn thống kê có gì mà hiện.
///
/// Vì sao cần riêng seeder này: <see cref="DemoDataSeeder"/> và <see cref="PerfDataSeeder"/> rải
/// dữ liệu quanh thời điểm CHÚNG chạy. Xem lại sau vài tuần thì mọi mốc đã trôi vào quá khứ —
/// Bàn làm việc hiện "doanh thu 7 ngày = 0", "không có chuyến nào khởi hành", "hôm nay không có
/// lịch hẹn". Màn hình trông như hỏng dù dữ liệu vẫn đúng.
///
/// Seeder này chỉ thêm dữ liệu trong CỬA SỔ ±14 ngày quanh hôm nay:
///   · phiếu thu đã ghi nhận rải 14 ngày qua  → biểu đồ doanh thu + so sánh tuần trước
///   · chuyến khởi hành rải 30 ngày tới       → thẻ tour hôm nay / trong tháng / dải 7 ngày
///   · lịch chăm sóc hẹn hôm nay và tuần này  → "Lịch hôm nay", cột "Hôm nay/Ngày mai" ở Kanban
///   · công việc đến hạn trong tuần           → thẻ "Việc của tôi"
///
/// Idempotent theo MÃ: đã có phiếu thu mã "RC-NOW-…" trong 14 ngày qua thì bỏ qua. Chạy lại vào
/// ngày khác sẽ bơm thêm cho cửa sổ mới — đúng ý "dữ liệu luôn tươi khi mở lên xem".
/// TÁI SỬ DỤNG danh mục sẵn có (khách/đơn/nhân sự), không tạo bảng hay cột nào.
/// </summary>
public static class RecentDataSeeder
{
    private const string ReceiptPrefix = "RC-NOW-";
    private const string DeparturePrefix = "DEP-NOW-";
    private const string CarePrefix = "Nhắc hẹn hôm nay";
    private const string TaskPrefix = "CV-NOW-";

    private static readonly string[] PaymentMethods = ["cash", "bank", "momo", "vnpay"];

    public static async Task SeedAsync(AppDbContext db, AmbientTenantContext ambient)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == DemoDataSeeder.DemoSlug && !t.IsDeleted);
        if (tenant is null)
        {
            return;
        }

        ambient.SetTenant(tenant.Id);

        var today = new DateTimeOffset(DateTimeOffset.Now.Date, TimeSpan.Zero);
        var from = today.AddDays(-14);

        var orderIds = await db.Orders.Select(o => o.Id).Take(400).ToListAsync();
        var customerIds = await db.Set<Customer>().Select(c => c.Id).Take(400).ToListAsync();
        var userIds = await db.Users.Select(u => u.Id).Take(20).ToListAsync();
        if (orderIds.Count == 0 || customerIds.Count == 0)
        {
            return;
        }

        // Thẻ "Việc của tôi" và "Lịch hôm nay" lọc theo NGƯỜI ĐANG ĐĂNG NHẬP. Gán cho một user
        // bất kỳ thì hai thẻ đó vẫn rỗng — phải gán đúng tài khoản admin demo hay dùng để xem.
        var adminId = await db.Users.Where(u => u.Email == "admin@demo.vn").Select(u => u.Id).FirstOrDefaultAsync();
        if (adminId == Guid.Empty && userIds.Count > 0)
        {
            adminId = userIds[0];
        }

        var rng = new Random(20260807);
        T Pick<T>(IReadOnlyList<T> xs) => xs[rng.Next(xs.Count)];

        var added = 0;

        // Mỗi khối tự kiểm tra riêng: chạy lại chỉ bù đúng phần còn thiếu, không nhân đôi phần đã có.
        var needReceipts = !await db.ReceiptVouchers.AnyAsync(r => r.IssuedAt >= from && r.Code.StartsWith(ReceiptPrefix));
        var needDepartures = !await db.Set<TourDeparture>().AnyAsync(t => t.DepartureDate >= today && t.Code.StartsWith(DeparturePrefix));
        var needCares = !await db.Set<CustomerCare>().AnyAsync(c => c.RemindAt == today && c.AssignedToUserId == adminId);
        var needTasks = !await db.Set<WorkTask>().AnyAsync(t => t.Code!.StartsWith(TaskPrefix) && t.AssigneeUserId == adminId);
        if (!needReceipts && !needDepartures && !needCares && !needTasks)
        {
            return;
        }

        // ---- 1) Phiếu thu đã ghi nhận, rải đều 14 ngày qua -----------------------
        // 14 ngày (không phải 7) để thẻ "so với tuần trước" có SỐ THẬT ở cả hai kỳ.
        // Số tiền dao động mạnh để đường biểu đồ có hình, không phẳng lì.
        for (var d = needReceipts ? 13 : -1; d >= 0; d--)
        {
            var day = today.AddDays(-d);
            var perDay = rng.Next(3, 9);
            for (var i = 0; i < perDay; i++)
            {
                db.ReceiptVouchers.Add(new ReceiptVoucher
                {
                    Code = $"{ReceiptPrefix}{day:yyMMdd}-{i:D2}",
                    Title = "Thu tiền tour",
                    IssuedAt = day.AddHours(rng.Next(8, 18)),
                    OrderId = Pick(orderIds),
                    Amount = rng.Next(3, 90) * 1_000_000m,
                    PaymentMethod = Pick(PaymentMethods),
                    Partner = null,
                    Status = 1,
                    IsRecognized = true,
                });
                added++;
            }
        }

        // ---- 2) Chuyến khởi hành trong 7 ngày tới --------------------------------
        string[] places = ["Đà Nẵng - Hội An", "Nha Trang - Đà Lạt", "Phú Quốc", "Hà Nội - Sapa",
                           "Hạ Long - Yên Tử", "Quy Nhơn - Phú Yên", "Côn Đảo", "Huế - Quảng Bình"];
        string[] types = ["Nội địa", "Inbound", "Outbound"];
        var dep = 0;
        // 30 ngày để cả dải "Tour khởi hành trong tháng" lẫn dải 7 ngày đều có dữ liệu.
        for (var d = 0; needDepartures && d <= 30; d++)
        {
            var day = today.AddDays(d);
            var perDay = d <= 6 ? rng.Next(1, 5) : rng.Next(0, 3);
            for (var i = 0; i < perDay; i++)
            {
                var nights = rng.Next(2, 6);
                var slots = rng.Next(16, 45);
                db.Set<TourDeparture>().Add(new TourDeparture
                {
                    Code = $"{DeparturePrefix}{day:yyMMdd}-{i:D2}",
                    Title = $"{Pick(places)} {nights + 1}N{nights}Đ",
                    TourType = Pick(types),
                    Category = rng.Next(0, 3),
                    DepartureDate = day,
                    EndDate = day.AddDays(nights),
                    TotalSlots = slots,
                    AmountAdults = rng.Next(5, slots),
                    AssignedToUserId = userIds.Count > 0 ? Pick(userIds) : null,
                    Status = 1,
                });
                dep++;
            }
        }

        // Vài chuyến ĐANG diễn ra (khởi hành trước hôm nay, kết thúc sau hôm nay).
        for (var i = 0; needDepartures && i < 3; i++)
        {
            var start = today.AddDays(-rng.Next(1, 4));
            db.Set<TourDeparture>().Add(new TourDeparture
            {
                Code = $"{DeparturePrefix}RUN-{i:D2}",
                Title = $"{Pick(places)} (đang chạy)",
                TourType = Pick(types),
                Category = rng.Next(0, 3),
                DepartureDate = start,
                EndDate = today.AddDays(rng.Next(1, 4)),
                TotalSlots = rng.Next(16, 40),
                AssignedToUserId = userIds.Count > 0 ? Pick(userIds) : null,
                Status = 1,
            });
            dep++;
        }

        // ---- 3) Lịch chăm sóc hẹn hôm nay / tuần này -----------------------------
        string[] careTitles = ["Gọi xác nhận khởi hành", "Nhắc thanh toán đợt 2", "Chăm sóc sau tour",
                               "Gửi báo giá tour hè", "Xác nhận danh sách khách", "Mời tham gia chương trình ưu đãi"];
        var cares = 0;
        for (var i = 0; needCares && i < 8; i++)
        {
            db.Set<CustomerCare>().Add(new CustomerCare
            {
                CustomerId = Pick(customerIds),
                Title = $"{Pick(careTitles)}",
                Detail = CarePrefix,
                RemindAt = today,
                AssignedToUserId = adminId,
                Status = rng.Next(0, 2),
            });
            cares++;
        }
        for (var d = 1; needCares && d <= 6; d++)
        {
            for (var i = 0; i < rng.Next(1, 4); i++)
            {
                db.Set<CustomerCare>().Add(new CustomerCare
                {
                    CustomerId = Pick(customerIds),
                    Title = Pick(careTitles),
                    RemindAt = today.AddDays(d),
                    AssignedToUserId = i == 0 ? adminId : (userIds.Count > 0 ? Pick(userIds) : null),
                    Status = rng.Next(0, 2),
                });
                cares++;
            }
        }

        // ---- 4) Công việc đến hạn trong tuần -------------------------------------
        string[] taskTitles = ["Chuẩn bị hồ sơ đoàn", "Tổng kết doanh thu tuần", "Duyệt booking khách đoàn",
                               "Kiểm tra hợp đồng đối tác", "Báo cáo công nợ tuần", "Đặt vé máy bay đoàn"];
        var tasks = 0;
        for (var i = 0; needTasks && i < 10; i++)
        {
            db.Set<WorkTask>().Add(new WorkTask
            {
                Code = $"{TaskPrefix}{i:D3}",
                Title = Pick(taskTitles),
                AssigneeUserId = adminId,
                CreatedByUserId = adminId,
                StartDate = today.AddDays(-rng.Next(0, 3)),
                DueDate = today.AddDays(rng.Next(0, 7)),
                Priority = rng.Next(0, 3),
                Progress = rng.Next(0, 100),
                Status = rng.Next(0, 2),
            });
            tasks++;
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Console.WriteLine($"[RecentSeed] Đã bơm dữ liệu quanh hôm nay: {added} phiếu thu · {dep} chuyến · {cares} lịch hẹn · {tasks} công việc.");
    }
}
