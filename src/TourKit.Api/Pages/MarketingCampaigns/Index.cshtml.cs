using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.MarketingCampaigns;

// CRUD offcanvas: CampaignDto scalar (Name/Channel enum/Subject/Body/Status), không FK/collection con.
// Create bỏ qua Status (mặc định nháp); Update dùng Status. Gửi (SendAsync) không đưa vào màn này.
[Authorize(Policy = "marketing.view")]
public class IndexModel : PageModel
{
    private readonly ICampaignService _svc;
    public IndexModel(ICampaignService svc) => _svc = svc;

    public IReadOnlyList<CampaignDto> Items { get; private set; } = [];
    public CampaignStatsDto Stats { get; private set; } = new(0, 0, 0, 0);

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string Name { get; set; } = "";
        public int Channel { get; set; } = (int)MarketingChannel.Email;
        public string? Subject { get; set; }
        public string Body { get; set; } = "";
        public int Status { get; set; }
    }

    public static string ChannelLabel(MarketingChannel c) => c switch
    {
        MarketingChannel.Email => "Email",
        MarketingChannel.Sms => "SMS",
        MarketingChannel.Zalo => "Zalo",
        _ => c.ToString(),
    };

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Stats = await _svc.GetStatsAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        var channel = (MarketingChannel)Input.Channel;
        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCampaignDto(Input.Name, channel, Input.Subject, Input.Body, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateCampaignDto(Input.Name, channel, Input.Subject, Input.Body));
        }

        return new JsonResult(Result.Success("Đã lưu chiến dịch."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá chiến dịch.";
        return RedirectToPage();
    }
}
