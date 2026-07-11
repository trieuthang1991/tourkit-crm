using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Finance;

namespace TourKit.Api.Controllers;

/// <summary>
/// Quy trình duyệt cấu hình được (legacy ApprovalProcess) dưới /api/v1/approval-processes —
/// template duyệt: bước theo chức vụ + người duyệt mỗi bước, admin dựng như dữ liệu.
/// </summary>
[ApiController]
[Route("api/v1/approval-processes")]
public sealed class ApprovalProcessesController(IApprovalProcessService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ApprovalProcessView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.ApprovalProcessView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> Create([FromBody] CreateApprovalProcessDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/approval-processes/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApprovalProcessDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/steps")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> AddStep(Guid id, [FromBody] AddStepDto dto)
        => Ok(await service.AddStepAsync(id, dto));

    [HttpDelete("{id:guid}/steps/{stepId:guid}")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> DeleteStep(Guid id, Guid stepId)
    {
        await service.DeleteStepAsync(id, stepId);
        return NoContent();
    }

    [HttpPost("{id:guid}/steps/reorder")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> ReorderSteps(Guid id, [FromBody] ReorderStepsDto dto)
    {
        await service.ReorderStepsAsync(id, dto);
        return NoContent();
    }

    [HttpPut("{id:guid}/steps/{stepId:guid}/users")]
    [Authorize(Permissions.ApprovalProcessManage)]
    public async Task<IActionResult> SetStepUsers(Guid id, Guid stepId, [FromBody] SetStepUsersDto dto)
    {
        await service.SetStepUsersAsync(id, stepId, dto);
        return NoContent();
    }
}
