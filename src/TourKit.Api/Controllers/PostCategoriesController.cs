using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Content;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục bài viết (legacy CategoriesPost) dưới /api/v1/post-categories.</summary>
[ApiController]
[Route("api/v1/post-categories")]
public sealed class PostCategoriesController(IPostCategoryService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.PostView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Create([FromBody] CreatePostCategoryDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/post-categories/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.PostManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostCategoryDto dto)
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
