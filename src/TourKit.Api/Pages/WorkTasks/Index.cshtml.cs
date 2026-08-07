using TourKit.Api.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Work;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.WorkTasks;

// Danh sách công việc: DataTables SERVER-SIDE (không get-all).
// Cùng trang phục vụ 2 mục menu: "Danh sách Công việc" (/cong-viec = tất cả) và
// "Công việc của tôi" (/cong-viec/cua-toi = lọc theo người đăng nhập). Phân biệt qua đoạn route "pham_vi".
[Authorize(Policy = "task.view")]
public class IndexModel : TkListPageModel
{
    private readonly IWorkTaskService _svc;
    private readonly UserDirectory _users;
    public IndexModel(IWorkTaskService svc, UserDirectory users)
    {
        _svc = svc;
        _users = users;
    }

    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    /// <summary>Phạm vi "của tôi" (đoạn route pham_vi = "cua-toi"): việc được giao cho tôi HOẶC do tôi tạo — bám hệ cũ.</summary>
    public bool IsMine => RouteData.Values.TryGetValue("pham_vi", out var v) && v as string == "cua-toi";

    /// <summary>Tiêu đề/heading theo phạm vi đang xem.</summary>
    public string Heading => IsMine ? "Công việc của tôi" : "Danh sách công việc";

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        public string? Description { get; set; }
        public Guid? AssigneeUserId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int Priority { get; set; } = (int)WorkTaskPriority.Normal;
        public int Progress { get; set; }
        public int Status { get; set; } = (int)WorkTaskStatus.Todo;
    }

    public static string StatusLabel(int s) => ((WorkTaskStatus)s) switch
    {
        WorkTaskStatus.Todo => "Cần làm",
        WorkTaskStatus.InProgress => "Đang làm",
        WorkTaskStatus.Done => "Hoàn thành",
        WorkTaskStatus.Cancelled => "Huỷ",
        _ => "—",
    };

    public static string StatusColor(int s) => ((WorkTaskStatus)s) switch
    {
        WorkTaskStatus.Done => "success",
        WorkTaskStatus.InProgress => "info",
        WorkTaskStatus.Cancelled => "secondary",
        _ => "warning",
    };

    public static string PriorityLabel(int p) => ((WorkTaskPriority)p) switch
    {
        WorkTaskPriority.High => "Cao",
        WorkTaskPriority.Low => "Thấp",
        _ => "Bình thường",
    };

    public static string PriorityColor(int p) => ((WorkTaskPriority)p) switch
    {
        WorkTaskPriority.High => "danger",
        WorkTaskPriority.Low => "secondary",
        _ => "info",
    };

    /// <summary>Thống kê cho dashboard "Công việc của tôi" (chỉ nạp khi ở phạm vi của tôi).</summary>
    public WorkTaskStatsDto? Stats { get; private set; }

    public async Task OnGetAsync()
    {
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        // Cả hai phạm vi đều cần dải thống kê đầu màn — "của tôi" đếm theo tôi, danh sách chung đếm tất cả.
        Stats = await _svc.GetStatsAsync(mineScope: IsMine);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var status = int.TryParse(Request.Query["status"], out var st) ? st : (int?)null;
        var assignee = Guid.TryParse(Request.Query["assigneeUserId"], out var au) ? au : (Guid?)null;
        var priority = int.TryParse(Request.Query["priority"], out var pr) ? pr : (int?)null;
        // "Của tôi" (mineScope): service lọc việc được giao HOẶC do tôi tạo, theo id từ claim — không tra bảng.
        var result = await _svc.ListAsync(dt.Page, dt.Size, assignee, status, dt.Keyword, priority, mineScope: IsMine);

        // Ngày nghiệp vụ hôm nay (neo offset 0) để tính "Cảnh báo" quá hạn — bám cột staging.
        var today = TkDate.Day(DateTimeOffset.Now);
        var data = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code ?? "—",
            title = x.Title,
            description = x.Description,
            assigneeUserId = x.AssigneeUserId,
            assigneeName = x.AssigneeName,
            createdByName = x.CreatedByName,
            workflowName = x.WorkflowName,
            startDate = x.StartDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            startDateText = x.StartDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—",
            dueDate = x.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            dueDateText = x.DueDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—",
            priority = x.Priority,
            priorityLabel = PriorityLabel(x.Priority),
            priorityColor = PriorityColor(x.Priority),
            progress = x.Progress,
            // Cảnh báo quá hạn: có hạn, đã qua hạn, và chưa Hoàn thành/Huỷ (tính toán — không lưu schema).
            overdue = x.DueDate is { } dd && dd < today && x.Status != (int)WorkTaskStatus.Done && x.Status != (int)WorkTaskStatus.Cancelled,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        });

        return DtJson(dt.Draw, result.Total, result.Total, data);
    }

    /// <summary>Một cột của bảng Kanban — nạp theo TỪNG cột và từng trang, không get-all.</summary>
    public async Task<IActionResult> OnGetKanbanColumnAsync(int status, int page = 1, int size = 15)
    {
        if (size is < 1 or > 50)
        {
            size = 15;
        }

        var keyword = Request.Query["q"].ToString() is { Length: > 0 } q ? q : null;
        var assignee = Guid.TryParse(Request.Query["assigneeUserId"], out var a) ? a : (Guid?)null;
        var priority = int.TryParse(Request.Query["priority"], out var p) ? p : (int?)null;
        var result = await _svc.ListAsync(page, size, assignee, status, keyword, priority, mineScope: IsMine);

        var today = TkDate.Day(DateTimeOffset.Now);
        var cards = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code ?? "—",
            title = x.Title,
            description = x.Description,
            assigneeUserId = x.AssigneeUserId,
            assigneeName = x.AssigneeName,
            startDate = x.StartDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            dueDate = x.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            dueDateText = x.DueDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
            priority = x.Priority,
            priorityLabel = PriorityLabel(x.Priority),
            priorityColor = PriorityColor(x.Priority),
            progress = x.Progress,
            overdue = x.DueDate is { } dd && dd < today && x.Status != (int)WorkTaskStatus.Done && x.Status != (int)WorkTaskStatus.Cancelled,
            status = x.Status,
        });

        return new JsonResult(new { total = result.Total, page, size, hasMore = page * size < result.Total, cards });
    }

    /// <summary>Kéo–thả sang cột khác trên Kanban → chỉ đổi trạng thái.</summary>
    public async Task<IActionResult> OnPostMoveAsync(Guid id, int status)
    {
        try
        {
            await _svc.MoveAsync(id, status);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã chuyển \"" + StatusLabel(status) + "\"."));
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateWorkTaskDto(
                Input.Title, Input.Description, Input.AssigneeUserId, TkDate.Day(Input.DueDate),
                Input.Priority, Input.Status, null, null, null,
                TkDate.Day(Input.StartDate), Input.Progress));
        }
        else
        {
            await _svc.CreateAsync(new CreateWorkTaskDto(
                Input.Title, Input.Description, Input.AssigneeUserId, TkDate.Day(Input.DueDate),
                Input.Priority, Input.Status, null, null, null,
                TkDate.Day(Input.StartDate), Input.Progress));
        }

        return new JsonResult(Result.Success("Đã lưu công việc."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá công việc.";
        return RedirectToPage();
    }
}
