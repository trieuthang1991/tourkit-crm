using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Application.Admin;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Work;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Workspace;

[Authorize(Policy = "report.dashboard.view")]
public class IndexModel : PageModel
{
    private readonly IWorkTaskService _tasks;
    private readonly ICustomerCareService _cares;
    private readonly ICurrentUser _current;
    private readonly IUserAdminService _users;
    public IndexModel(IWorkTaskService tasks, ICustomerCareService cares, ICurrentUser current, IUserAdminService users)
    {
        _tasks = tasks;
        _cares = cares;
        _current = current;
        _users = users;
    }

    public IReadOnlyList<WorkTaskDto> MyTasks { get; private set; } = [];
    public IReadOnlyList<CustomerCareDto> MyCares { get; private set; } = [];
    public int OpenTaskCount { get; private set; }
    public int UpcomingCareCount { get; private set; }
    public int OverdueTaskCount { get; private set; }
    public int DoneTaskCount { get; private set; }
    public string UserName { get; private set; } = "bạn";

    public static string TaskStatusLabel(int s) => TourKit.Api.Pages.WorkTasks.IndexModel.StatusLabel(s);
    public static string TaskStatusColor(int s) => TourKit.Api.Pages.WorkTasks.IndexModel.StatusColor(s);

    public static string PriorityLabel(int p) => ((WorkTaskPriority)p) switch
    {
        WorkTaskPriority.Low => "Thấp",
        WorkTaskPriority.High => "Cao",
        _ => "Bình thường",
    };

    public async Task OnGetAsync()
    {
        var uid = _current.UserId;
        if (uid is Guid g)
        {
            MyTasks = (await _tasks.ListAsync(1, 100, g, null)).Items
                .OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue).ToList();
            MyCares = (await _cares.ListAsync(1, 100, new CustomerCareListFilter(AssignedToUserId: g))).Items
                .OrderBy(c => c.RemindAt ?? DateTimeOffset.MaxValue).ToList();
            UserName = (await _users.ListAsync()).FirstOrDefault(u => u.Id == g)?.FullName ?? "bạn";
        }

        var now = DateTimeOffset.UtcNow;
        OpenTaskCount = MyTasks.Count(t => t.Status is (int)WorkTaskStatus.Todo or (int)WorkTaskStatus.InProgress);
        OverdueTaskCount = MyTasks.Count(t =>
            t.Status is (int)WorkTaskStatus.Todo or (int)WorkTaskStatus.InProgress && t.DueDate is { } d && d < now);
        DoneTaskCount = MyTasks.Count(t => t.Status == (int)WorkTaskStatus.Done);
        UpcomingCareCount = MyCares.Count(c => c.Status != 1); // 1 = Đã xong
    }

    // Care status: 0 Chờ xử lý · 1 Đã xong.
    public static string CareStatusLabel(int s) => s == 1 ? "Đã xong" : "Chờ xử lý";
    public static string CareStatusColor(int s) => s == 1 ? "success" : "warning";
}
