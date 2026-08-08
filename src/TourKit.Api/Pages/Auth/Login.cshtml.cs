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
    public LoginModel(ICookieAuthService auth)
    {
        _auth = auth;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var principal = await _auth.AuthenticateAsync(Input.Email, Input.Password);
        if (principal is null)
        {
            Error = "Email hoặc mật khẩu không đúng.";
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = Input.RememberMe });
        return LocalRedirect(returnUrl ?? "/tong-quan");
    }
}
