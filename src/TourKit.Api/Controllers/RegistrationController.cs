using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Application.Provisioning;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/registration")]
[AllowAnonymous]
public sealed class RegistrationController(IProvisioningService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest body)
    {
        var outcome = await svc.RegisterAsync(body);
        return outcome.Error switch
        {
            RegistrationError.None =>
                Created($"/api/v1/tenants/{outcome.Response!.TenantId}", outcome.Response),
            RegistrationError.SlugTaken =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Slug đã được dùng."),
            RegistrationError.EmailTaken =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Email đã được sử dụng."),
            RegistrationError.Conflict =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Thông tin đăng ký đã được sử dụng."),
            _ => ValidationProblem("Thiếu thông tin hoặc mật khẩu < 8 ký tự."),
        };
    }
}
