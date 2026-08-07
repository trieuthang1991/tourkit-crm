using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Content;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Notifications;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;
using TourKit.Application.Work;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Workspace;

// Bàn làm việc — bám ĐỦ các khối của bản cũ (web/src/features/workspace/WorkspacePage.tsx):
// thao tác nhanh · 4 KPI · vòng tròn tỉ lệ công việc · thông báo · công nợ khách hàng ·
// lịch hẹn hôm nay · phiếu thu/chi chờ duyệt · thông tin doanh nghiệp (bài viết) ·
// công việc của tôi · lịch khởi hành.
// Màn RIÊNG với /Dashboard (Tổng quan điều hành) — không gộp.
//
// Nguyên tắc nạp: đếm lấy từ *StatsDto (1 truy vấn gộp), danh sách chỉ lấy TRANG NHỎ để hiển thị.
// KHÔNG kéo 100 bản ghi về rồi tự đếm ở bộ nhớ như bản trước.
[Authorize]
public class IndexModel : PageModel
{
    /// <summary>Số dòng hiển thị trong mỗi thẻ danh sách — thẻ là chỗ liếc nhanh, không phải bảng đầy đủ.</summary>
    private const int CardRows = 6;
    private const int CareDone = 2;
    private const int VoucherPending = 0;

    private readonly IWorkTaskService _tasks;
    private readonly ICustomerCareService _cares;
    private readonly ICurrentUser _current;
    private readonly INotificationService _notifications;
    private readonly IReportService _reports;
    private readonly ICustomerService _customers;
    private readonly IReceiptService _receipts;
    private readonly IPaymentService _payments;
    private readonly IPostService _posts;
    private readonly IDepartureService _departures;

    public IndexModel(
        IWorkTaskService tasks, ICustomerCareService cares, ICurrentUser current,
        INotificationService notifications, IReportService reports, ICustomerService customers,
        IReceiptService receipts, IPaymentService payments, IPostService posts,
        IDepartureService departures)
    {
        _departures = departures;
        _tasks = tasks;
        _cares = cares;
        _current = current;
        _notifications = notifications;
        _reports = reports;
        _customers = customers;
        _receipts = receipts;
        _payments = payments;
        _posts = posts;
    }

    public string UserName { get; private set; } = "bạn";

    // --- KPI ---
    public DashboardSummaryDto Summary { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    public int CustomerCount { get; private set; }

    // --- Công việc ---
    public WorkTaskStatsDto TaskStats { get; private set; } = new(0, 0, 0, 0, 0, 0);
    public IReadOnlyList<WorkTaskDto> MyTasks { get; private set; } = [];

    // --- CSKH ---
    public IReadOnlyList<CustomerCareDto> MyCares { get; private set; } = [];
    public IReadOnlyList<CustomerCareDto> TodayCares { get; private set; } = [];

    // --- Các thẻ còn lại ---
    public IReadOnlyList<NotificationDto> Notifications { get; private set; } = [];
    public IReadOnlyList<OrderDebtRowDto> TopDebt { get; private set; } = [];
    public IReadOnlyList<ReceiptListItemDto> PendingReceipts { get; private set; } = [];
    public IReadOnlyList<PaymentListItemDto> PendingPayments { get; private set; } = [];
    public int PendingReceiptTotal { get; private set; }
    public int PendingPaymentTotal { get; private set; }
    public IReadOnlyList<PostDto> Posts { get; private set; } = [];

    // --- Nhịp doanh thu + chuyến khởi hành (bản dựng lại theo mẫu chủ dự án gửi) ---
    public WorkspacePulseDto Pulse { get; private set; } = new([], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    /// <summary>Chuyến khởi hành trong 7 ngày tới — thẻ ngang "Tour khởi hành trong tuần".</summary>
    public IReadOnlyList<DepartureDto> WeekDepartures { get; private set; } = [];
    /// <summary>Số chuyến khởi hành theo TỪNG ngày trong 7 ngày tới — dải lịch cuối màn.</summary>
    public IReadOnlyList<(DateTimeOffset Day, int Count)> DepartureDays { get; private set; } = [];
    /// <summary>Số chuyến khởi hành trong HÔM NAY (thẻ KPI "Tour hôm nay").</summary>
    public int TodayDepartures { get; private set; }
    /// <summary>Số chuyến ĐANG diễn ra hôm nay (đã khởi hành, chưa kết thúc).</summary>
    public int RunningDepartures { get; private set; }
    /// <summary>Tổng chỗ + hướng dẫn viên cần cho 7 ngày tới (3 số tổng dưới dải lịch).</summary>
    public int WeekSlots { get; private set; }

    /// <summary>Lịch hẹn của tôi đã QUÁ hạn — một dòng của "Trung tâm cần xử lý".</summary>
    public int OverdueCares { get; private set; }
    /// <summary>Số đơn đang còn nợ (từ báo cáo công nợ) — dòng "Công nợ quá hạn".</summary>
    public int DebtOrders { get; private set; }
    /// <summary>Tên khách của các dòng công nợ đang hiển thị (báo cáo chỉ trả CustomerId).</summary>
    public Dictionary<Guid, string> DebtNames { get; } = [];

    /// <summary>Mức tăng/giảm so với kỳ trước, đơn vị %. null = kỳ trước bằng 0, không so được.</summary>
    public static decimal? Delta(decimal now, decimal before)
        => before == 0 ? null : Math.Round((now - before) / before * 100, 1);

    public bool CanTask => User.HasClaim("perm", "task.view");
    public bool CanCare => User.HasClaim("perm", "care.view");
    public bool CanDashboard => User.HasClaim("perm", "report.dashboard.view");
    public bool CanReceipt => User.HasClaim("perm", "receipt.view");
    public bool CanPayment => User.HasClaim("perm", "payment.view");
    public bool CanPost => User.HasClaim("perm", "post.view");
    public bool CanDeparture => User.HasClaim("perm", "departure.view");
    public bool CanLead => User.HasClaim("perm", "lead.view");
    public bool CanCustomer => User.HasClaim("perm", "customer.view");
    public bool CanBooking => User.HasClaim("perm", "booking.view");

    public static string TaskStatusLabel(int s) => TourKit.Api.Pages.WorkTasks.IndexModel.StatusLabel(s);
    public static string TaskStatusColor(int s) => TourKit.Api.Pages.WorkTasks.IndexModel.StatusColor(s);

    public static string PriorityLabel(int p) => ((WorkTaskPriority)p) switch
    {
        WorkTaskPriority.Low => "Thấp",
        WorkTaskPriority.High => "Cao",
        _ => "Bình thường",
    };

    // Trạng thái CSKH (bám CARE_STATUS hệ cũ): 0 Mới · 1 Đang xử lý · 2 Hoàn thành.
    public static string CareStatusLabel(int s) => s switch
    {
        1 => "Đang xử lý",
        CareDone => "Hoàn thành",
        _ => "Mới",
    };

    public static string CareStatusColor(int s) => s switch
    {
        1 => "info",
        CareDone => "success",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        // Tên hiển thị lấy thẳng từ claim trong cookie đăng nhập — KHÔNG truy vấn.
        // (Trước đây nạp TOÀN BỘ bảng Users chỉ để lọc ra đúng một cái tên.)
        UserName = User.FindFirst("name")?.Value is { Length: > 0 } n ? n : "bạn";

        var uid = _current.UserId;
        if (uid is Guid g)
        {
            if (CanTask)
            {
                // Đếm lấy từ stats; danh sách chỉ lấy đúng số dòng thẻ hiển thị.
                TaskStats = await _tasks.GetStatsAsync();
                MyTasks = (await _tasks.ListAsync(1, CardRows, g, null)).Items
                    .OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue).ToList();
            }

            if (CanCare)
            {
                MyCares = (await _cares.ListAsync(1, CardRows, new CustomerCareListFilter(AssignedToUserId: g))).Items
                    .OrderBy(c => c.RemindAt ?? DateTimeOffset.MaxValue).ToList();

                // "Lịch hẹn hôm nay": lọc trong nhóm việc CHƯA xong của chính mình. Lấy rộng hơn thẻ
                // một chút vì phải sàng theo ngày, nhưng vẫn là trang nhỏ chứ không phải cả bảng.
                var soon = (await _cares.ListAsync(1, 50, new CustomerCareListFilter(AssignedToUserId: g))).Items;
                var today = DateTimeOffset.Now.Date;
                TodayCares = soon
                    .Where(c => c.Status != CareDone && c.RemindAt is { } r && r.ToLocalTime().Date == today)
                    .OrderBy(c => c.RemindAt)
                    .ToList();

                OverdueCares = soon.Count(c => c.Status != CareDone && c.RemindAt is { } r && r.ToLocalTime().Date < today);
            }
        }

        Notifications = (await _notifications.ListMineAsync(unreadOnly: false, take: 10)).ToList();

        if (CanDashboard)
        {
            Summary = await _reports.GetDashboardAsync();
            CustomerCount = (await _customers.ListAsync(1, 1)).Total;
            Pulse = await _reports.GetWorkspacePulseAsync(7);

            // Công nợ khách hàng: báo cáo trả về theo đơn còn nợ, chỉ giữ 6 khoản lớn nhất.
            var debt = await _reports.GetOrderDebtAsync();
            DebtOrders = debt.Count(d => d.Outstanding > 0);
            TopDebt = debt.OrderByDescending(d => d.Outstanding).Take(5).ToList();

            // Báo cáo công nợ chỉ trả CustomerId. Thẻ này nói về "ai đang nợ" nên phải có TÊN —
            // tra đúng 5 khách của 5 dòng đang hiện, không nạp cả bảng.
            foreach (var d in TopDebt)
            {
                try { DebtNames[d.CustomerId] = (await _customers.GetAsync(d.CustomerId)).FullName; }
                catch (Exception) { /* khách đã xoá — để trống, dòng vẫn hiện mã đơn */ }
            }
        }

        if (CanDeparture)
        {
            // Chuyến trong 7 ngày tới — lấy MỘT trang có biên, không get-all.
            var today = TkDate.Day(DateTimeOffset.Now);
            var week = (await _departures.ListAsync(1, 60, new DepartureListFilter(
                DepartureFrom: today, DepartureTo: today.AddDays(6), Sort: "dateAsc"))).Items;

            WeekDepartures = week.Take(10).ToList();
            WeekSlots = week.Sum(d => d.TotalSlots);
            DepartureDays = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(i))
                .Select(day => (Day: day, Count: week.Count(d => d.DepartureDate is { } dd && TkDate.Day(dd) == day)))
                .ToList();
            TodayDepartures = DepartureDays.Count > 0 ? DepartureDays[0].Count : 0;

            // Đang diễn ra = đã khởi hành và chưa kết thúc (một trang có biên quanh hôm nay).
            var running = (await _departures.ListAsync(1, 60, new DepartureListFilter(
                DepartureTo: today, EndFrom: today))).Items;
            RunningDepartures = running.Count;
        }

        if (CanReceipt)
        {
            var r = await _receipts.ListAllAsync(1, CardRows, new ReceiptListFilter(Status: VoucherPending));
            PendingReceipts = r.Items;
            PendingReceiptTotal = r.Total;
        }

        if (CanPayment)
        {
            var p = await _payments.ListAllAsync(1, CardRows, new PaymentListFilter(Status: VoucherPending));
            PendingPayments = p.Items;
            PendingPaymentTotal = p.Total;
        }

        if (CanPost)
        {
            Posts = (await _posts.ListAsync(1, 8, null, null)).Items;
        }
    }
}
