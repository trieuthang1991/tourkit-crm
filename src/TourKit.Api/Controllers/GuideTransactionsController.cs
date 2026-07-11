using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Operations;

namespace TourKit.Api.Controllers;

/// <summary>Thu-chi HDV theo chuyến (legacy RevenueExpensesInTourGuide) dưới /api/v1/guide-assignments/{id}/transactions.</summary>
[ApiController]
[Route("api/v1/guide-assignments/{assignmentId:guid}/transactions")]
public sealed class GuideTransactionsController(IGuideTransactionService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.GuideView)]
    public async Task<IActionResult> Get(Guid assignmentId) => Ok(await service.GetByAssignmentAsync(assignmentId));

    [HttpPost]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Create(Guid assignmentId, [FromBody] CreateGuideTransactionDto dto)
    {
        var created = await service.CreateAsync(assignmentId, dto);
        return Created($"/api/v1/guide-assignments/{assignmentId}/transactions/{created.Id}", created);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Delete(Guid assignmentId, Guid id)
    {
        await service.DeleteAsync(assignmentId, id);
        return NoContent();
    }
}
