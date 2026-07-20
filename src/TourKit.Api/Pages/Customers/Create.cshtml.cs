using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

/// <summary>View-model dùng chung Create/Edit — các trường cốt lõi của khách hàng.</summary>
public sealed class CustomerFormInput
{
    [Required(ErrorMessage = "Bắt buộc nhập họ tên")]
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int CustomerType { get; set; }
    public string? Source { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
}

[Authorize(Policy = "customer.view")]
public class CreateModel : PageModel
{
    private readonly ICustomerService _service;
    public CreateModel(ICustomerService service) => _service = service;

    [BindProperty] public CustomerFormInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _service.CreateAsync(new CreateCustomerDto(
            FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
            Source: Input.Source, Email: Input.Email, Address: Input.Address, City: Input.City));
        TempData["ok"] = "Đã thêm khách hàng.";
        return RedirectToPage("Index");
    }
}
