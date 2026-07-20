using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerService _service;
    public IndexModel(ICustomerService service) => _service = service;

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public int? CustomerType { get; set; }
    [BindProperty(SupportsGet = true)] public string? Source { get; set; }
    [BindProperty(SupportsGet = true)] public string? City { get; set; }
    [BindProperty(SupportsGet = true, Name = "page")] public int PageNo { get; set; } = 1;

    public const int PageSize = 20;
    public PagedResult<CustomerDto> Result { get; private set; } = new(Array.Empty<CustomerDto>(), 0, 1, PageSize);
    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } =
        new([], [], [], [], [], [], [], [], [], []);

    public async Task OnGetAsync()
    {
        var filter = new CustomerListFilter(Q: Q, CustomerType: CustomerType, Source: Source, City: City);
        Result = await _service.ListAsync(PageNo < 1 ? 1 : PageNo, PageSize, filter);
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
    }

    public int TotalPages => (int)Math.Ceiling(Result.Total / (double)PageSize);

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách hàng.";
        return RedirectToPage(new { page = PageNo, Q, Source, City });
    }
}
