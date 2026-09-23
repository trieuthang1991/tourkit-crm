using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Work;

namespace TourKit.Api.Pages.Workflows;

[Authorize(Policy = "workflow.view")]
public class IndexModel : PageModel
{
    private readonly IWorkflowService _svc;
    public IndexModel(IWorkflowService svc) => _svc = svc;

    public IReadOnlyList<WorkflowDto> Items { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int Status { get; set; }
    }

    // Trạng thái dự án: 0 đang dùng · 1 lưu trữ.
    public static string StatusLabel(int s) => s == 1 ? "Lưu trữ" : "Đang dùng";
    public static string StatusColor(int s) => s == 1 ? "secondary" : "success";

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

    /// <summary>Đổi nhanh trạng thái dự án (lưu trữ / khôi phục) từ nút trên dòng.</summary>
    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, int status)
    {
        await _svc.SetStatusAsync(id, status);
        TempData["ok"] = status == 1 ? "Đã lưu trữ dự án." : "Đã khôi phục dự án.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateWorkflowDto(Input.Name, TkDate.Day(Input.StartDate), TkDate.Day(Input.EndDate), Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateWorkflowDto(Input.Name, TkDate.Day(Input.StartDate), TkDate.Day(Input.EndDate)));
        }

        return new JsonResult(Result.Success("Đã lưu dự án."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá dự án.";
        return RedirectToPage();
    }
}
