using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/tour-ratings")]
public sealed class TourRatingsController(ITourRatingService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.RatingView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.RatingView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var rating = await service.GetAsync(id);
        return Ok(rating);
    }

    [HttpPost]
    [Authorize(Permissions.RatingManage)]
    public async Task<IActionResult> Create([FromBody] CreateTourRatingDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.RatingManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTourRatingDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.RatingManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
