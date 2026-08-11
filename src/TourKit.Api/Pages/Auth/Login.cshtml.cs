using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Application.Auth;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ICookieAuthService _auth;
    private readonly IExternalAuthService _external;
    private readonly IAuthenticationSchemeProvider _schemes;

    public LoginModel(
        ICookieAuthService auth,
        IExternalAuthService external,
        IAuthenticationSchemeProvider schemes)
    {
        _auth = auth;
        _external = external;
        _schemes = schemes;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    /// <summary>
    /// Nút Google hiện hay không hỏi thẳng danh sách scheme ĐÃ ĐĂNG KÝ, không hỏi lại file cấu hình.
    ///
    /// Bản đầu tiên đọc <c>IOptions&lt;GoogleAuthOptions&gt;</c> để quyết định hiện nút, trong khi
    /// việc đăng ký scheme lại đọc cấu hình ở thời điểm dựng ứng dụng. Hai đường đọc khác nhau thì
    /// có lúc lệch nhau — và lệch theo đúng chiều tệ nhất: nút hiện lên, người dùng bấm, hệ thống
    /// ném "No authentication handler is registered for the scheme 'Google'" và trả về lỗi 500.
    /// Hỏi đúng cái mình sắp dùng thì không còn khoảng cách nào để lệch.
    /// </summary>
    public bool GoogleEnabled { get; private set; }

    public async Task OnGetAsync() => await CapNhatTrangThaiGoogleAsync();

    private async Task CapNhatTrangThaiGoogleAsync() =>
        GoogleEnabled = await _schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is not null;

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await CapNhatTrangThaiGoogleAsync();
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
        return LocalRedirect(NoiDenAnToan(returnUrl));
    }

    /// <summary>Bắt đầu chặng đi Google.</summary>
    public async Task<IActionResult> OnPostGoogleAsync(string? returnUrl = null)
    {
        await CapNhatTrangThaiGoogleAsync();
        if (!GoogleEnabled)
        {
            // 404 chứ không phải thông báo lỗi: chưa cấu hình thì đường này coi như không tồn tại,
            // và người ngoài không dò được hệ thống có sẵn tính năng gì mà chưa bật.
            return NotFound();
        }

        // returnUrl được làm sạch NGAY tại đây rồi mới cất vào properties. Nếu để nguyên chuỗi người
        // dùng gửi lên và chỉ kiểm lúc quay về, thì giữa hai thời điểm đó nó đã kịp đi qua Google và
        // quay lại — thêm một chặng nữa để sai sót.
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Page("/Auth/Login", "GoogleCallback", new { returnUrl = NoiDenAnToan(returnUrl) }),
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>Google trả người dùng về đây.</summary>
    public async Task<IActionResult> OnGetGoogleCallbackAsync(string? returnUrl = null)
    {
        await CapNhatTrangThaiGoogleAsync();
        if (!GoogleEnabled)
        {
            return NotFound();
        }

        var ketQua = await HttpContext.AuthenticateAsync(ExternalAuthDefaults.Scheme);
        if (!ketQua.Succeeded || !GoogleIdentityReader.TryRead(ketQua.Principal, out var danhTinh))
        {
            return await ThatBaiAsync("Không nhận được thông tin từ Google. Vui lòng thử lại.");
        }

        var ketCuc = await _external.ResolveAsync(danhTinh!);
        switch (ketCuc.Status)
        {
            case ExternalAuthStatus.Authenticated when ketCuc.UserId is { } userId:
                var principal = await _auth.CreatePrincipalAsync(userId);
                if (principal is null)
                {
                    return await ThatBaiAsync("Tài khoản không còn hoạt động.");
                }

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        // Không ghi nhớ, khớp mặc định của đường email/mật khẩu (ô "Ghi nhớ đăng nhập"
                        // mặc định TẮT). Trước đây đường Google luôn IsPersistent = true, nên cùng một
                        // hệ thống mà hai lối vào cho hai mức ghi nhớ khác nhau: vào bằng Google là
                        // cookie nằm lại trên đĩa, người dùng không có cách nào chọn "chỉ phiên này" —
                        // rộng hơn mong đợi trên máy dùng chung.
                        //
                        // Quay về từ Google thì không còn form để đọc lựa chọn của người dùng, nên lấy
                        // mức CHẶT hơn làm mặc định.
                        IsPersistent = false,
                    });

                // Xoá cookie tạm ngay khi đã có cookie chính: để lại thì người dùng còn cầm một vé
                // "email này đã xác minh" đi lại được thêm 10 phút mà không việc gì phải cầm nữa.
                await HttpContext.SignOutAsync(ExternalAuthDefaults.Scheme);
                return LocalRedirect(NoiDenAnToan(returnUrl));

            case ExternalAuthStatus.NeedsOnboarding:
                // Email hợp lệ nhưng chưa thuộc công ty nào — GIỮ cookie tạm, vì trang đăng ký sẽ đọc
                // lại email đã xác minh từ đó chứ không tin ô nhập trên form.
                return RedirectToPage("/Auth/Register");

            default:
                return await ThatBaiAsync(ketCuc.Error ?? "Không thể đăng nhập bằng Google.");
        }
    }

    /// <summary>Dọn cookie tạm rồi quay về trang đăng nhập kèm thông báo chung.</summary>
    private async Task<IActionResult> ThatBaiAsync(string thongBao)
    {
        await HttpContext.SignOutAsync(ExternalAuthDefaults.Scheme);
        TempData["LoginError"] = thongBao;
        return RedirectToPage("/Auth/Login");
    }

    /// <summary>
    /// Chỉ nhận đường dẫn nội bộ. Bỏ qua bước này thì trang đăng nhập thành bàn đạp chuyển hướng:
    /// gửi ai đó link /dang-nhap?returnUrl=https://trang-gia.example rồi họ đăng nhập xong bị đá
    /// sang trang giả mạo, mà thanh địa chỉ lúc đầu vẫn đúng tên miền thật.
    /// </summary>
    private string NoiDenAnToan(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/tong-quan";
}
