using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Commission;

namespace TourKit.Api.Pages.CommissionTiers;

// LIST read-only: CreateCommissionCampaignDto cần collection con động (Tiers) + nhiều FK nhân viên (UserIds) —
// vượt khuôn CRUD offcanvas scalar, nên chỉ liệt kê chính sách + xem chi tiết (nhân viên áp dụng, các bậc).
// ICommissionCampaignService.ListAsync() KHÔNG nhận filter/paging → phân trang tại page model
// (server-side với DataTables) và ẩn ô search mặc định.
// Status: 0 = đang áp dụng, 1 = ngừng (theo CommissionCampaignService.ResolveRateAsync/EnsureNoOverlapAsync).
[Authorize(Policy = "commission.view")]
public class IndexModel : TkListPageModel
{
    private readonly ICommissionCampaignService _svc;
    public IndexModel(ICommissionCampaignService svc) => _svc = svc;

    public static string StatusLabel(int s) => s == 0 ? "Đang áp dụng" : "Ngừng";
    public static string StatusColor(int s) => s == 0 ? "success" : "secondary";

    // Không có OnGet: dữ liệu bảng nạp hoàn toàn qua ?handler=Data (server-side).

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var all = await _svc.ListAsync();

        // Từ khoá: khớp tên chính sách. Service trả TOÀN BỘ (không phân trang) nên lọc ngay ở page model
        // trước khi cắt trang — không cần đổi service (StringComparison lọc ở bộ nhớ).
        if (dt.Keyword is { } kw)
        {
            all = all.Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var data = all.Skip((dt.Page - 1) * dt.Size).Take(dt.Size).Select(x => new
        {
            id = x.Id,
            name = x.Name,
            startDateText = x.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            endDateText = x.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            userCount = x.UserCount,
            tierCount = x.TierCount,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        });

        return DtJson(dt.Draw, all.Count, all.Count, data);
    }

    /// <summary>Chi tiết chính sách (chỉ xem): nhân viên áp dụng + bảng bậc lợi nhuận.</summary>
    public async Task<IActionResult> OnGetDetailAsync(Guid campaignId)
    {
        var d = await _svc.GetAsync(campaignId);
        return new JsonResult(new
        {
            name = d.Name,
            rangeText = d.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " → " + d.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            statusLabel = StatusLabel(d.Status),
            statusColor = StatusColor(d.Status),
            userNames = d.UserNames,
            tiers = d.Tiers.Select(t => new { startAmount = t.StartAmount, endAmount = t.EndAmount, percentage = t.Percentage }),
        });
    }
}
