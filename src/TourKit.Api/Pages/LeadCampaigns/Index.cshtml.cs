using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Pages.LeadCampaigns;

[Authorize(Policy = "lead.view")]
public class IndexModel : PageModel
{
    private readonly ILeadCampaignService _svc;
    public IndexModel(ILeadCampaignService svc) => _svc = svc;

    public IReadOnlyList<LeadCampaignDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public string? Note { get; set; }
    }

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        await _svc.CreateAsync(new CreateLeadCampaignDto(Input.Name, Input.Note));
        return new JsonResult(Result.Success("Đã lưu chiến dịch chia số."));
    }
}
