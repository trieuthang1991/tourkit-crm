using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Work;

namespace TourKit.Api.Controllers;

/// <summary>Công việc nội bộ (legacy Tasking) dưới /api/v1/work-tasks — giao/theo dõi việc trong tenant.</summary>
[ApiController]
[Route("api/v1/work-tasks")]
public sealed class WorkTasksController(IWorkTaskService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.TaskView)]
    public async Task<IActionResult> List([FromQuery] Guid? assigneeUserId, [FromQuery] int? status)
        => Ok(await service.ListAsync(assigneeUserId, status));

    [HttpPost]
    [Authorize(Permissions.TaskManage)]
    public async Task<IActionResult> Create([FromBody] CreateWorkTaskDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/work-tasks/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.TaskManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkTaskDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.TaskManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
