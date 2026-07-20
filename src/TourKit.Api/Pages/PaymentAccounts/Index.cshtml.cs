using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.PaymentAccounts;

[Authorize(Policy = "paymentaccount.view")]
public class IndexModel : PageModel
{
    private readonly IPaymentAccountService _svc;
    public IndexModel(IPaymentAccountService svc) => _svc = svc;

    public IReadOnlyList<PaymentAccountDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên tài khoản")] public string Name { get; set; } = "";
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; }
        public string? Branch { get; set; }
        public string? TransferNote { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdatePaymentAccountDto(Input.Name, Input.BankName, Input.AccountNumber, Input.AccountHolder, Input.Branch, Input.TransferNote, Input.IsDefault, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreatePaymentAccountDto(Input.Name, Input.BankName, Input.AccountNumber, Input.AccountHolder, Input.Branch, Input.TransferNote, Input.IsDefault, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu tài khoản."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá tài khoản.";
        return RedirectToPage();
    }
}
