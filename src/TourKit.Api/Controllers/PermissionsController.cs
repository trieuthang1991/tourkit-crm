using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Admin;

namespace TourKit.Api.Controllers;

/// <summary>Catalog quyền (global) dưới /api/v1/permissions — cho màn gán quyền của vai trò.</summary>
[ApiController]
[Route("api/v1/permissions")]
public sealed class PermissionsController(IRoleAdminService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.UserView)]
    public async Task<IActionResult> List() => Ok(await service.ListPermissionsAsync());
}
