using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/tours/{tourId:guid}/assignees")]
public sealed class TourAssigneesController(ITourAssigneeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TourView)]
    public async Task<IActionResult> List(Guid tourId)
    {
        var assignees = await service.ListAsync(tourId);
        return Ok(assignees);
    }

    [HttpPut]
    [Authorize(Permissions.TourUpdate)]
    public async Task<IActionResult> Replace(Guid tourId, [FromBody] AssigneeDto[] assignees)
    {
        await service.ReplaceAsync(tourId, assignees);
        return NoContent();
    }
}
