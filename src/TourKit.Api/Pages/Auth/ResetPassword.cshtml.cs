using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Auth;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly IPasswordResetService _svc;
    public ResetPasswordModel(IPasswordResetService svc) => _svc = svc;

    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mật khẩu mới")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Bắt buộc nhập lại mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; } = "";
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return RedirectToPage("/Auth/ForgotPassword");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var error = await _svc.ResetAsync(Token ?? "", Input.Password);
        if (error is not null)
        {
            Error = error;
            return Page();
        }

        TempData["ok"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToPage("/Auth/Login");
    }
}
