using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20,
        [FromQuery] CustomerListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    // Thẻ thống kê đầu màn (bám hệ cũ): tổng KH / tạo hôm nay / tháng này / mua lần đầu / mua lại.
    [HttpGet("stats")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    // Giá trị có sẵn cho dropdown lọc (nguồn, tag, chi nhánh, chiến dịch...) — user chọn thay vì gõ tay.
    [HttpGet("filter-options")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> FilterOptions() => Ok(await service.GetFilterOptionsAsync());

    // Phễu khách hàng (đếm theo phân nhóm) + chăm sóc (mua lần đầu/mua lại + N ngày chưa liên hệ).
    [HttpGet("funnel")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Funnel() => Ok(await service.GetFunnelAsync());

    // Rà khách nghi trùng (theo SĐT/email chuẩn hoá) để gộp thủ công.
    [HttpGet("duplicates")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Duplicates() => Ok(await service.FindDuplicatesAsync());

    // Xuất CSV TOÀN BỘ khách khớp bộ lọc (server-side, không giới hạn trang) — khác export client chỉ trang hiện tại.
    [HttpGet("export")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Export([FromQuery] CustomerListFilter? filter = null)
    {
        var all = await service.ListAsync(1, int.MaxValue, filter);
        var headers = new[] { "Mã KH", "Họ tên", "SĐT", "Email", "Nguồn", "Số lần mua", "Doanh thu", "Ngày tạo" };
        var rows = all.Items.Select(c => (IReadOnlyList<string?>)new[]
        {
            c.Code, c.FullName, c.Phone, c.Email, c.Source,
            c.PurchaseCount.ToString(CultureInfo.InvariantCulture),
            c.Revenue.ToString(CultureInfo.InvariantCulture),
            c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        });
        var bytes = CsvBuilder.Build(headers, rows);
        return File(bytes, "text/csv", "khach-hang.csv");
    }

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var customer = await service.GetAsync(id);
        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Permissions.CustomerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CustomerDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
