using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Settings;

namespace TourKit.Api.Controllers;

/// <summary>
/// Hồ sơ công ty (legacy Config) dưới /api/v1/company-profile — singleton mỗi tenant. GET cho mọi người
/// dùng trong tenant (in hợp đồng/báo giá cần), PUT yêu cầu quyền company.manage.
/// </summary>
[ApiController]
[Route("api/v1/company-profile")]
public sealed class CompanyProfileController(ICompanyProfileService service) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get() => Ok(await service.GetAsync());

    [HttpPut]
    [Authorize(Permissions.CompanyManage)]
    public async Task<IActionResult> Save([FromBody] CompanyProfileDto dto)
    {
        await service.SaveAsync(dto);
        return NoContent();
    }
}
