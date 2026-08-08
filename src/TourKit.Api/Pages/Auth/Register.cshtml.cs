using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Application.Auth;
using TourKit.Application.Provisioning;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly IProvisioningService _svc;
    private readonly ICookieAuthService _auth;

    public RegisterModel(IProvisioningService svc, ICookieAuthService auth)
    {
        _svc = svc;
        _auth = auth;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    /// <summary>Đang hoàn tất đăng ký cho người vừa xác minh danh tính qua Google.</summary>
    public bool IsGoogleRegistration { get; private set; }

    /// <summary>Email đã được Google xác minh. Hiện ở dạng chỉ đọc, không cho sửa.</summary>
    public string? GoogleEmail { get; private set; }

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

    public async Task OnGetAsync()
    {
        var danhTinh = await DocDanhTinhNgoaiAsync();
        if (danhTinh is null)
        {
            return;
        }

        IsGoogleRegistration = true;
        GoogleEmail = danhTinh.Email;
        Input.AdminEmail = danhTinh.Email;
        Input.AdminFullName = danhTinh.DisplayName ?? "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Đọc lại cookie tạm ở MỖI request, không tin một ô ẩn nào trên form. Nếu chế độ Google được
        // quyết định bởi thứ gửi lên từ trình duyệt, thì ai cũng tự khai mình là "đã xác minh Google"
        // với email bất kỳ và chiếm được email của người khác — chính là thứ cookie tạm sinh ra để chặn.
        var danhTinh = await DocDanhTinhNgoaiAsync();
        IsGoogleRegistration = danhTinh is not null;
        GoogleEmail = danhTinh?.Email;

        if (danhTinh is not null)
        {
            // Chế độ Google không có ô mật khẩu, và email lấy từ claim chứ không lấy từ form — nên
            // hai lỗi kiểm tra đó không phản ánh gì về dữ liệu người dùng thật sự nhập.
            ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.AdminPassword)}");
            ModelState.Remove($"{nameof(Input)}.{nameof(InputModel.AdminEmail)}");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = danhTinh is null
            ? await _svc.RegisterAsync(new RegisterTenantRequest(
                Input.CompanyName.Trim(), Input.Slug.Trim().ToLowerInvariant(),
                Input.AdminEmail.Trim(), Input.AdminPassword, Input.AdminFullName.Trim()))
            : await _svc.RegisterExternalAsync(new RegisterExternalTenantRequest(
                Input.CompanyName.Trim(), Input.Slug.Trim().ToLowerInvariant(),
                danhTinh.Email,                       // email của Google, KHÔNG phải ô nhập trên form
                Input.AdminFullName.Trim(),
                danhTinh.Provider, danhTinh.Subject));

        switch (outcome.Error)
        {
            case RegistrationError.None when danhTinh is not null:
                return await VaoThangAsync(outcome.Response!);
            case RegistrationError.None:
                TempData["ok"] = "Tạo doanh nghiệp thành công. Đăng nhập bằng email và mật khẩu vừa đặt.";
                return RedirectToPage("/Auth/Login");
            case RegistrationError.SlugTaken:
                Error = "Mã doanh nghiệp đã được sử dụng, vui lòng chọn mã khác.";
                return Page();
            case RegistrationError.EmailTaken:
                Error = "Email đã được sử dụng, vui lòng đăng nhập hoặc dùng email khác.";
                return Page();
            case RegistrationError.Conflict:
                Error = "Thông tin đăng ký vừa được sử dụng. Vui lòng kiểm tra lại và thử lại.";
                return Page();
            default:
                Error = danhTinh is null
                    ? "Thiếu thông tin hoặc mật khẩu dưới 8 ký tự."
                    : "Thiếu thông tin đăng ký.";
                return Page();
        }
    }

    /// <summary>
    /// Đăng ký bằng Google xong thì vào thẳng, không bắt đăng nhập lại: mật khẩu là chuỗi ngẫu nhiên
    /// đã bị vứt bỏ nên không có gì để họ nhập, và họ vừa chứng minh danh tính cách đây vài giây.
    /// </summary>
    private async Task<IActionResult> VaoThangAsync(RegistrationResponse ketQua)
    {
        var principal = await _auth.CreatePrincipalAsync(ketQua.AdminUserId);
        if (principal is null)
        {
            await HttpContext.SignOutAsync(ExternalAuthDefaults.Scheme);
            TempData["LoginError"] = "Đã tạo doanh nghiệp nhưng chưa đăng nhập được. Vui lòng đăng nhập lại.";
            return RedirectToPage("/Auth/Login");
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
        await HttpContext.SignOutAsync(ExternalAuthDefaults.Scheme);
        return LocalRedirect("/tong-quan");
    }

    private async Task<ExternalIdentity?> DocDanhTinhNgoaiAsync()
    {
        var ketQua = await HttpContext.AuthenticateAsync(ExternalAuthDefaults.Scheme);
        return ketQua.Succeeded && GoogleIdentityReader.TryRead(ketQua.Principal, out var danhTinh)
            ? danhTinh
            : null;
    }
}
