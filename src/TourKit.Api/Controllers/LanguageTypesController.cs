using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Danh mục ngôn ngữ HDV (legacy LanguagesType) dưới /api/v1/language-types — dùng quyền HDV.</summary>
[ApiController]
[Route("api/v1/language-types")]
public sealed class LanguageTypesController(ILanguageTypeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.GuideView)]
    public async Task<IActionResult> List()
    {
        var items = await service.ListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Create([FromBody] CreateLanguageTypeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/language-types/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLanguageTypeDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
