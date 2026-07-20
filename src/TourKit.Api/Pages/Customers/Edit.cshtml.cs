using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class EditModel : PageModel
{
    private readonly ICustomerService _service;
    public EditModel(ICustomerService service) => _service = service;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CustomerFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var c = await _service.GetAsync(Id);
        Input = new CustomerFormInput
        {
            FullName = c.FullName,
            Phone = c.Phone,
            Email = c.Email,
            CustomerType = c.CustomerType,
            Source = c.Source,
            City = c.City,
            Address = c.Address,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _service.UpdateAsync(Id, new UpdateCustomerDto(
            FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
            Source: Input.Source, Email: Input.Email, Address: Input.Address, City: Input.City));
        TempData["ok"] = "Đã cập nhật khách hàng.";
        return RedirectToPage("Index");
    }
}
