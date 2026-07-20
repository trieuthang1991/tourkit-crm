using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Pages.CustomerCommissionRules;

[Authorize(Policy = "commission.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerCommissionRuleService _svc;
    private readonly ICustomerTypeService _types;
    public IndexModel(ICustomerCommissionRuleService svc, ICustomerTypeService types)
    {
        _svc = svc;
        _types = types;
    }

    public IReadOnlyList<CustomerCommissionRuleDto> Items { get; private set; } = [];
    public IReadOnlyList<CustomerTypeDto> Types { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public int CustomerType { get; set; }
        public decimal Percentage { get; set; }
        public int Status { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Types = await _types.ListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCustomerCommissionRuleDto(Input.Percentage, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateCustomerCommissionRuleDto(Input.CustomerType, Input.Percentage, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu hoa hồng theo loại khách."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá hoa hồng theo loại khách.";
        return RedirectToPage();
    }
}
