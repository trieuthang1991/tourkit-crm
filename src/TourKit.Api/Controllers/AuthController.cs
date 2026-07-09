using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Auth;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        var result = await auth.LoginAsync(body);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Đăng nhập thất bại.");
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body)
    {
        var result = await auth.RefreshAsync(body.RefreshToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token không hợp lệ.");
        }

        return Ok(result);
    }
}
