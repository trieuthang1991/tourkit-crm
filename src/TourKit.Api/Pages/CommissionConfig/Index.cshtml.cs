using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Pages.CommissionConfig;

// CRUD offcanvas: CommissionRuleDto scalar (UserId enrich UserName, Percentage, Status) — 1 FK có lookup user.
// Update chỉ đổi Percentage/Status (UpdateCommissionRuleDto không nhận UserId) — như CustomerCommissionRules.
[Authorize(Policy = "commission.view")]
public class IndexModel : PageModel
{
    private readonly ICommissionRuleService _svc;
    private readonly IUserAdminService _users;
    public IndexModel(ICommissionRuleService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

    public IReadOnlyList<CommissionRuleDto> Items { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public CommissionRuleStatsDto Stats { get; private set; } = new(0, 0, 0, 0m);

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public Guid UserId { get; set; }
        public decimal Percentage { get; set; }
        public int Status { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCommissionRuleDto(Input.Percentage, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateCommissionRuleDto(Input.UserId, Input.Percentage, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu quy tắc hoa hồng."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá quy tắc hoa hồng.";
        return RedirectToPage();
    }
}
