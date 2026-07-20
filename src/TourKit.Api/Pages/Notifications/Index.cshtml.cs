using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Notifications;

namespace TourKit.Api.Pages.Notifications;

[Authorize(Policy = "report.dashboard.view")]
public class IndexModel : PageModel
{
    private readonly INotificationService _svc;
    public IndexModel(INotificationService svc) => _svc = svc;

    public IReadOnlyList<NotificationDto> Items { get; private set; } = [];

    public async Task OnGetAsync() => Items = await _svc.ListMineAsync(false);

    public async Task<IActionResult> OnPostMarkReadAsync(Guid id)
    {
        await _svc.MarkReadAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsync()
    {
        await _svc.MarkAllReadAsync();
        TempData["ok"] = "Đã đánh dấu tất cả đã đọc.";
        return RedirectToPage();
    }
}
