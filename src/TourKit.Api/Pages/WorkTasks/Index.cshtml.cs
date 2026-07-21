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

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        public string? Description { get; set; }
        public Guid? AssigneeUserId { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int Priority { get; set; } = (int)WorkTaskPriority.Normal;
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

    public async Task OnGetAsync()
        => Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var status = int.TryParse(Request.Query["status"], out var st) ? st : (int?)null;
        var result = await _svc.ListAsync(dt.Page, dt.Size, null, status, dt.Keyword);

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            title = x.Title,
            description = x.Description,
            assigneeUserId = x.AssigneeUserId,
            dueDate = x.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            dueDateText = x.DueDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—",
            priority = x.Priority,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        });

        return DtJson(dt.Draw, result.Total, result.Total, data);
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
                Input.Priority, Input.Status, null, null, null));
        }
        else
        {
            await _svc.CreateAsync(new CreateWorkTaskDto(
                Input.Title, Input.Description, Input.AssigneeUserId, TkDate.Day(Input.DueDate),
                Input.Priority, Input.Status, null, null, null));
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
