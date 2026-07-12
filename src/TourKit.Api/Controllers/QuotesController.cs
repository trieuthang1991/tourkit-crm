using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Báo giá dưới /api/v1/quotes: CRUD header + dòng, tổng tự tính.</summary>
[ApiController]
[Route("api/v1/quotes")]
public sealed class QuotesController(IQuoteService service, IQuoteConversionService conversion) : ControllerBase
{
    /// <summary>Chuyển báo giá CHẤP NHẬN thành đơn + đặt dịch vụ lẻ (legacy DuyetBooking).</summary>
    [HttpPost("{id:guid}/convert")]
    [Authorize(Permissions.QuoteManage)]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertQuoteDto dto)
    {
        var result = await conversion.ConvertAsync(id, dto);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Permissions.QuoteView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] QuoteListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.QuoteView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.QuoteView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var quote = await service.GetAsync(id);
        return Ok(quote);
    }

    [HttpPost]
    [Authorize(Permissions.QuoteManage)]
    public async Task<IActionResult> Create([FromBody] CreateQuoteDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/quotes/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.QuoteManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuoteDto dto)
    {
        var updated = await service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.QuoteManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
