using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Provisioning;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly IProvisioningService _svc;
    public RegisterModel(IProvisioningService svc) => _svc = svc;

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên doanh nghiệp")] public string CompanyName { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập mã doanh nghiệp")]
        [RegularExpression("^[a-z0-9-]{3,40}$", ErrorMessage = "Mã chỉ gồm chữ thường, số và dấu gạch ngang (3–40 ký tự)")]
        public string Slug { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập họ tên")] public string AdminFullName { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string AdminEmail { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập mật khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string AdminPassword { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý với điều khoản sử dụng")]
        public bool AcceptTerms { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = await _svc.RegisterAsync(new RegisterTenantRequest(
            Input.CompanyName.Trim(), Input.Slug.Trim().ToLowerInvariant(),
            Input.AdminEmail.Trim(), Input.AdminPassword, Input.AdminFullName.Trim()));

        switch (outcome.Error)
        {
            case RegistrationError.None:
                TempData["ok"] = "Tạo doanh nghiệp thành công. Đăng nhập bằng email và mật khẩu vừa đặt.";
                return RedirectToPage("/Auth/Login");
            case RegistrationError.SlugTaken:
                Error = "Mã doanh nghiệp đã được sử dụng, vui lòng chọn mã khác.";
                return Page();
            default:
                Error = "Thiếu thông tin hoặc mật khẩu dưới 8 ký tự.";
                return Page();
        }
    }
}
