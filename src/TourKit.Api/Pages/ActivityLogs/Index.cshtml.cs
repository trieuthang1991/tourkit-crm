using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Audit;
using TourKit.Application.Audit.Dtos;

namespace TourKit.Api.Pages.ActivityLogs;

[Authorize(Policy = "activitylog.view")]
public class IndexModel : PageModel
{
    private readonly IActivityLogService _svc;
    public IndexModel(IActivityLogService svc) => _svc = svc;

    public IReadOnlyList<ActivityLogDto> Items { get; private set; } = [];

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 200, null, null)).Items;
}
