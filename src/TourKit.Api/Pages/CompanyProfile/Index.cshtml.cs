using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Settings;

namespace TourKit.Api.Pages.CompanyProfile;

// Sửa hồ sơ công ty = sửa số tài khoản ngân hàng in trên hoá đơn/hợp đồng → đường chuyển hướng
// dòng tiền. [Authorize] trần cho phép BẤT KỲ người đăng nhập nào sửa; API vốn đã đòi company.manage.
[Authorize(Policy = "company.manage")]
public class IndexModel : PageModel
{
    private readonly ICompanyProfileService _svc;
    public IndexModel(ICompanyProfileService svc) => _svc = svc;

    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên công ty")] public string Name { get; set; } = "";
        public string? ShortName { get; set; }
        public string? Address { get; set; }
        public string? Hotline { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? TaxCode { get; set; }
        public string? LegalRepName { get; set; }
        public string? LegalRepTitle { get; set; }
        public string? LicenseNumber { get; set; }
        public string? BankAccount { get; set; }
    }

    public async Task OnGetAsync()
    {
        var p = await _svc.GetAsync();
        Input = new InputModel
        {
            Name = p.Name, ShortName = p.ShortName, Address = p.Address, Hotline = p.Hotline,
            Email = p.Email, Website = p.Website, TaxCode = p.TaxCode, LegalRepName = p.LegalRepName,
            LegalRepTitle = p.LegalRepTitle, LicenseNumber = p.LicenseNumber, BankAccount = p.BankAccount,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _svc.SaveAsync(new CompanyProfileDto(
            Input.Name, Input.ShortName, Input.Address, Input.Hotline, Input.Email, Input.Website,
            Input.TaxCode, Input.LegalRepName, Input.LegalRepTitle, Input.LicenseNumber, Input.BankAccount));
        TempData["ok"] = "Đã lưu thông tin công ty.";
        return RedirectToPage();
    }
}
