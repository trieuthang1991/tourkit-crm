using TourKit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Orders;

[Authorize(Policy = "booking.view")]
public class DetailModel : PageModel
{
    private readonly IBookingService _svc;
    private readonly UserDirectory _users;
    private readonly ICurrentUser _current;
    public DetailModel(IBookingService svc, UserDirectory users, ICurrentUser current)
    {
        _svc = svc;
        _users = users;
        _current = current;
    }

    public OrderDto Order { get; private set; } = default!;
    public IReadOnlyList<BookingLineDto> Lines { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> SalesUsers { get; private set; } = [];

    public static string StatusLabel(OrderStatus s) => IndexModel.StatusLabel(s);
    public static string StatusColor(OrderStatus s) => IndexModel.StatusColor(s);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var loaded = await LoadAsync(id);
        return loaded ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(Guid id)
    {
        try
        {
            Order = await _svc.GetOrderAsync(id);
        }
        catch (Exception)
        {
            return false;
        }

        Lines = await _svc.ListOrderLinesAsync(id);
        SalesUsers = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        return true;
    }

    public async Task<IActionResult> OnPostAssignSalesAsync(Guid id, Guid? salesUserId)
    {
        await _svc.AssignSalesAsync(id, new AssignSalesDto(salesUserId));
        TempData["ok"] = "Đã cập nhật nhân viên phụ trách.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        if (_current.UserId is not Guid uid)
        {
            TempData["err"] = "Không xác định được người dùng hiện tại.";
            return RedirectToPage(new { id });
        }

        try
        {
            await _svc.CloseOrderAsync(id, uid);
            TempData["ok"] = "Đã tất toán đơn.";
        }
        catch (AppException ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReopenAsync(Guid id)
    {
        try
        {
            await _svc.ReopenOrderAsync(id);
            TempData["ok"] = "Đã mở lại đơn.";
        }
        catch (AppException ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage(new { id });
    }
}
