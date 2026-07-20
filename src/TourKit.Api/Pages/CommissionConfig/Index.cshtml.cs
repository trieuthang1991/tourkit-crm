using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Pages.CommissionConfig;

// Danh sách quy tắc hoa hồng theo nhân viên: DataTables SERVER-SIDE (không get-all) + giữ đủ thông tin
// bản cũ (web/src/features/commission/CommissionRulesPage.tsx): 4 KPI, lọc từ khoá tên NV + nhân viên +
// trạng thái (CommissionRuleListFilter), cột Nhân viên / Tỉ lệ (%) / Trạng thái, nút Sửa + Xoá.
// CRUD offcanvas: CommissionRuleDto scalar (UserId enrich UserName, Percentage, Status) — 1 FK có lookup user.
// Update chỉ đổi Percentage/Status (UpdateCommissionRuleDto không nhận UserId) — như CustomerCommissionRules.
[Authorize(Policy = "commission.view")]
public class IndexModel : TkListPageModel
{
    private readonly ICommissionRuleService _svc;
    private readonly IUserAdminService _users;
    public IndexModel(ICommissionRuleService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

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
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang, mọi tiêu chí đẩy xuống service.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var q = Request.Query;
        Guid? userId = Guid.TryParse(q["userId"], out var u) ? u : null;
        int? status = int.TryParse(q["status"], out var st) ? st : null;

        var result = await _svc.ListAsync(dt.Page, dt.Size, new CommissionRuleListFilter(dt.Keyword, userId, status));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            userId = x.UserId,
            userName = x.UserName ?? "—",
            percentage = x.Percentage,
            status = x.Status,
            statusLabel = x.Status == 1 ? "Đang áp dụng" : "Tạm ngừng",
            statusColor = x.Status == 1 ? "success" : "secondary",
        });

        return DtJson(dt.Draw, stats.Total, result.Total, data);
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
