using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerService _service;
    public IndexModel(ICustomerService service) => _service = service;

    // Thẻ thống kê + facet cho lần render đầu (DataTables lo phần bảng/search/paging qua ajax OnGetData).
    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } =
        new([], [], [], [], [], [], [], [], [], []);

    public async Task OnGetAsync()
    {
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
    }

    /// <summary>Nguồn dữ liệu cho DataTables (server-side processing). Trả JSON đúng định dạng DataTables.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var q = Request.Query;
        var draw = ParseInt(q["draw"], 0);
        var start = ParseInt(q["start"], 0);
        var length = ParseInt(q["length"], 20);
        if (length <= 0) { length = 20; }

        var search = q["search[value]"].ToString();
        var source = q["source"].ToString();
        var city = q["city"].ToString();
        var customerType = int.TryParse(q["customerType"], out var ct) ? ct : (int?)null;

        var filter = new CustomerListFilter(
            Q: string.IsNullOrWhiteSpace(search) ? null : search,
            CustomerType: customerType,
            Source: string.IsNullOrWhiteSpace(source) ? null : source,
            City: string.IsNullOrWhiteSpace(city) ? null : city);

        var page = (start / length) + 1;
        var result = await _service.ListAsync(page, length, filter);
        var stats = await _service.GetStatsAsync();   // tổng chưa lọc (recordsTotal)

        var data = result.Items.Select(c => new
        {
            id = c.Id,
            code = c.Code,
            fullName = c.FullName,
            phone = c.Phone,
            source = c.Source,
            tag = c.Tag,
            revenue = c.Revenue,
            createdAt = c.CreatedAt.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        });

        return new JsonResult(new
        {
            draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách hàng.";
        return RedirectToPage();
    }

    private static int ParseInt(Microsoft.Extensions.Primitives.StringValues v, int fallback) =>
        int.TryParse(v.ToString(), out var n) ? n : fallback;
}
