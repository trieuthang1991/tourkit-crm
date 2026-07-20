using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Pages.Agents;

[Authorize(Policy = "agent.view")]
public class IndexModel : PageModel
{
    private readonly IAgentService _svc;
    public IndexModel(IAgentService svc) => _svc = svc;

    public IReadOnlyList<AgentDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public decimal CreditLimit { get; set; }
        public int Status { get; set; } = 1;
    }

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateAgentDto(Input.Code, Input.Name, Input.ContactPerson, Input.Phone, Input.Email, Input.TaxCode, Input.Address, Input.CreditLimit, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateAgentDto(Input.Code, Input.Name, Input.ContactPerson, Input.Phone, Input.Email, Input.TaxCode, Input.Address, Input.CreditLimit, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu đại lý."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá đại lý.";
        return RedirectToPage();
    }
}
