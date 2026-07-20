using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.MessageTemplates;

[Authorize(Policy = "marketing.view")]
public class IndexModel : PageModel
{
    private readonly IMessageTemplateService _svc;
    public IndexModel(IMessageTemplateService svc) => _svc = svc;

    public IReadOnlyList<MessageTemplateDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public MarketingChannel Channel { get; set; }
        public string? Subject { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập nội dung")] public string Body { get; set; } = "";
    }

    public async Task OnGetAsync() => Items = await _svc.ListAsync(null);

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateMessageTemplateDto(Input.Name, Input.Channel, Input.Subject, Input.Body));
        }
        else
        {
            await _svc.CreateAsync(new CreateMessageTemplateDto(Input.Name, Input.Channel, Input.Subject, Input.Body));
        }

        return new JsonResult(Result.Success("Đã lưu mẫu tin nhắn."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá mẫu tin nhắn.";
        return RedirectToPage();
    }
}
