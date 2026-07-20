using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Pages.CommissionTiers;

// LIST read-only: CreateCommissionCampaignDto cần collection con động (Tiers) + nhiều FK nhân viên (UserIds) —
// vượt khuôn CRUD offcanvas scalar, nên chỉ liệt kê chính sách (tên/khoảng ngày/số NV/số bậc/trạng thái).
[Authorize(Policy = "commission.view")]
public class IndexModel : PageModel
{
    private readonly ICommissionCampaignService _svc;
    public IndexModel(ICommissionCampaignService svc) => _svc = svc;

    public IReadOnlyList<CommissionCampaignDto> Items { get; private set; } = [];

    public async Task OnGetAsync() => Items = await _svc.ListAsync();
}
