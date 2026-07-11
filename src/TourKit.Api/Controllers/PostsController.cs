using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Content;

namespace TourKit.Api.Controllers;

/// <summary>Bài viết/tin tức (legacy Posts) dưới /api/v1/posts.</summary>
[ApiController]
[Route("api/v1/posts")]
public sealed class PostsController(IPostService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.PostView)]
    public async Task<IActionResult> List([FromQuery] Guid? categoryId, [FromQuery] int? status)
        => Ok(await service.ListAsync(categoryId, status));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.PostView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/posts/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
