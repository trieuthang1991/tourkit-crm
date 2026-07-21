using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Auth;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _svc;
    public ForgotPasswordModel(IPasswordResetService svc) => _svc = svc;

    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Sent { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã doanh nghiệp")] public string TenantSlug { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _svc.SendResetLinkAsync(Input.TenantSlug.Trim(), Input.Email.Trim(),
            token => Url.Page("/Auth/ResetPassword", pageHandler: null, values: new { token }, protocol: Request.Scheme)!);

        // Luôn báo cùng một thông điệp — không tiết lộ email/mã doanh nghiệp nào tồn tại.
        Sent = true;
        return Page();
    }
}
