using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class DuplicatesModel : PageModel
{
    private readonly ICustomerService _service;
    public DuplicatesModel(ICustomerService service) => _service = service;

    public IReadOnlyList<DuplicateGroupDto> Groups { get; private set; } = [];

    public async Task OnGetAsync() => Groups = await _service.FindDuplicatesAsync();
}
