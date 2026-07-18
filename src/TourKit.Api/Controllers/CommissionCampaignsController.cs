using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Chính sách hoa hồng bậc thang (legacy CommissionCompaign) dưới /api/v1/commission-campaigns —
/// chính sách có tên + khoảng thời gian, gán nhóm nhân viên, gồm nhiều bậc lợi nhuận. Gate bằng quyền
/// hoa hồng sẵn có: xem = commission.view, tạo/sửa/xoá = commission.create.
/// </summary>
[ApiController]
[Route("api/v1/commission-campaigns")]
public sealed class CommissionCampaignsController(ICommissionCampaignService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpGet("resolve")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> Resolve(
        [FromQuery] Guid userId, [FromQuery] DateTimeOffset date, [FromQuery] decimal profit)
        => Ok(await service.ResolveRateAsync(userId, date, profit));

    [HttpPost]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCommissionCampaignDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/commission-campaigns/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommissionCampaignDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
