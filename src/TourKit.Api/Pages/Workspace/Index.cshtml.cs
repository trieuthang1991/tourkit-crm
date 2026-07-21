using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
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

    public IndexModel(
        IWorkTaskService tasks, ICustomerCareService cares, ICurrentUser current,
        INotificationService notifications, IReportService reports, ICustomerService customers,
        IReceiptService receipts, IPaymentService payments, IPostService posts)
    {
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
            }
        }

        Notifications = (await _notifications.ListMineAsync(unreadOnly: false)).Take(10).ToList();

        if (CanDashboard)
        {
            Summary = await _reports.GetDashboardAsync();
            CustomerCount = (await _customers.ListAsync(1, 1)).Total;

            // Công nợ khách hàng: báo cáo trả về theo đơn còn nợ, chỉ giữ 6 khoản lớn nhất.
            TopDebt = (await _reports.GetOrderDebtAsync())
                .OrderByDescending(d => d.Outstanding).Take(CardRows).ToList();
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
