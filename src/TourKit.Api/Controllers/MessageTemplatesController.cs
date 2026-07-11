using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Controllers;

/// <summary>Mẫu tin nhắn tái sử dụng (legacy Email_Sample/Marketing_Template) dưới /api/v1/message-templates.</summary>
[ApiController]
[Route("api/v1/message-templates")]
public sealed class MessageTemplatesController(IMessageTemplateService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.MarketingView)]
    public async Task<IActionResult> List([FromQuery] MarketingChannel? channel)
        => Ok(await service.ListAsync(channel));

    [HttpPost]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Create([FromBody] CreateMessageTemplateDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/message-templates/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMessageTemplateDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.MarketingCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
