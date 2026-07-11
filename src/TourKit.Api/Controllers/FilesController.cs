using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Files;

namespace TourKit.Api.Controllers;

/// <summary>Đính kèm tệp dưới /api/v1/files: upload (multipart), tải về, liệt kê. Nội dung ở IFileStorage.</summary>
[ApiController]
[Route("api/v1/files")]
public sealed class FilesController(IFileUploadService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.FileView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Permissions.FileManage)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var dto = await service.SaveAsync(file.FileName, file.ContentType, file.Length, stream, ct);
        return Created($"/api/v1/files/{dto.Id}", dto);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.FileView)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (meta, content) = await service.OpenAsync(id, ct);
        return File(content, meta.ContentType, meta.FileName);
    }
}
