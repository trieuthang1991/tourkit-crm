using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.CustomerCares;

[Authorize(Policy = "care.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerCareService _svc;
    private readonly IUserAdminService _users;
    private readonly ICustomerService _customers;
    public IndexModel(ICustomerCareService svc, IUserAdminService users, ICustomerService customers)
    {
        _svc = svc;
        _users = users;
        _customers = customers;
    }

    public IReadOnlyList<CustomerCareDto> Items { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public Guid CustomerId { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        public string? Detail { get; set; }
        public DateTimeOffset? RemindAt { get; set; }
        public string? Feedback { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public int Status { get; set; }
    }

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Nguồn cho Select2 ajax chọn khách (top 20 theo Q). Trả [{id,text}].</summary>
    public async Task<IActionResult> OnGetCustomerSearchAsync(string? q)
    {
        var result = await _customers.ListAsync(1, 20, new CustomerListFilter(Q: q));
        var items = result.Items.Select(c => new { id = c.Id, text = $"{c.FullName}{(string.IsNullOrEmpty(c.Phone) ? "" : " - " + c.Phone)}" });
        return new JsonResult(new { results = items });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCustomerCareDto(Input.Title, Input.Detail, Input.RemindAt, Input.Feedback, Input.AssignedToUserId, Input.Status));
        }
        else
        {
            if (Input.CustomerId == Guid.Empty)
            {
                return new JsonResult(Result.Error("Bắt buộc chọn khách hàng."));
            }

            await _svc.CreateAsync(new CreateCustomerCareDto(Input.CustomerId, Input.Title, Input.Detail, Input.RemindAt, Input.AssignedToUserId, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu lịch hẹn."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá lịch hẹn.";
        return RedirectToPage();
    }
}
