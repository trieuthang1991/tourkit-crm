using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Work;

namespace TourKit.Api.Controllers;

/// <summary>
/// Board Kanban cấu hình động (legacy Workflow/SectionWork) dưới /api/v1/workflows —
/// board + cột (trạng thái do người dùng tự đặt) + kéo thẻ việc giữa các cột.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
public sealed class WorkflowsController(IWorkflowService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.WorkflowView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.WorkflowView)]
    public async Task<IActionResult> Board(Guid id) => Ok(await service.GetBoardAsync(id));

    [HttpPost]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/workflows/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkflowDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/sections")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> AddSection(Guid id, [FromBody] CreateSectionDto dto)
        => Ok(await service.AddSectionAsync(id, dto));

    [HttpPut("{id:guid}/sections/{sectionId:guid}")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> UpdateSection(Guid id, Guid sectionId, [FromBody] UpdateSectionDto dto)
    {
        await service.UpdateSectionAsync(id, sectionId, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}/sections/{sectionId:guid}")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> DeleteSection(Guid id, Guid sectionId)
    {
        await service.DeleteSectionAsync(id, sectionId);
        return NoContent();
    }

    [HttpPost("{id:guid}/sections/reorder")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> ReorderSections(Guid id, [FromBody] ReorderSectionsDto dto)
    {
        await service.ReorderSectionsAsync(id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/tasks/{taskId:guid}/move")]
    [Authorize(Permissions.WorkflowManage)]
    public async Task<IActionResult> MoveTask(Guid id, Guid taskId, [FromBody] MoveTaskDto dto)
    {
        await service.MoveTaskAsync(id, taskId, dto);
        return NoContent();
    }
}
