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

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

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
