using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class DetailsModel : PageModel
{
    private readonly ICustomerService _service;
    private readonly ICustomerTypeService _types;
    private readonly ICustomerSourceService _sources;
    private readonly ICustomerTagService _tags;
    private readonly IMarketTypeService _markets;
    public DetailsModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _tags = tags;
        _markets = markets;
    }

    public CustomerDto Customer { get; private set; } = default!;
    public string TypeName { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Customer = await _service.GetAsync(id);
        await IndexModel.LoadCatalogsAsync(this, _types, _sources, _tags, _markets);
        var types = (IReadOnlyList<CustomerTypeDto>)ViewData["CustomerTypes"]!;
        TypeName = types.FirstOrDefault(t => t.Code == Customer.CustomerType)?.Name ?? "Khách lẻ";
        return Page();
    }

    /// <summary>2 chữ cái đầu cho avatar (khi không có ảnh).</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return "?"; }
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
