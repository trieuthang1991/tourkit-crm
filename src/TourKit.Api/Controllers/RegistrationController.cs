using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Provisioning;

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
            _ => ValidationProblem("Thiếu thông tin hoặc mật khẩu < 8 ký tự."),
        };
    }
}
