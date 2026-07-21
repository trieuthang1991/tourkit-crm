using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Auth;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ICookieAuthService _auth;
    private readonly IHostEnvironment _env;
    public LoginModel(ICookieAuthService auth, IHostEnvironment env)
    {
        _auth = auth;
        _env = env;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public sealed class InputModel
    {
        // KHÔNG đặt mặc định ở đây: giá trị này hiện thẳng trong ô "Mã doanh nghiệp" của MỌI khách
        // truy cập. Khách của tenant khác mà thấy sẵn "demo-tour" thì phải xoá đi mới nhập được.
        [Required] public string TenantSlug { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public void OnGet()
    {
        // Chỉ điền sẵn tenant demo khi chạy Development cho tiện thử; production để trống.
        if (_env.IsDevelopment())
        {
            Input.TenantSlug = "demo-tour";
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var principal = await _auth.AuthenticateAsync(Input.TenantSlug, Input.Email, Input.Password);
        if (principal is null)
        {
            Error = "Sai tài khoản, mật khẩu hoặc mã doanh nghiệp.";
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = Input.RememberMe });
        return LocalRedirect(returnUrl ?? "/tong-quan");
    }
}
