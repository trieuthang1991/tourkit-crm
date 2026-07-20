using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Api.Pages.FlightTicketsIndividual;

[Authorize(Policy = "ticketfund.view")]
public class IndexModel : PageModel
{
    private readonly IFlightTicketIndividualService _svc;
    public IndexModel(IFlightTicketIndividualService svc) => _svc = svc;

    public IReadOnlyList<FlightTicketIndividualDto> Items { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã vé")] public string Code { get; set; } = "";
        public string? TicketCode { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập PNR")] public string Pnr { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên khách")] public string CustomerName { get; set; } = "";
        public int TripType { get; set; }
        public string? Route { get; set; }
        public DateTimeOffset? DepartDate { get; set; }
        public DateTimeOffset? ReturnDate { get; set; }
        public decimal SellAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTimeOffset? PaymentDueDate { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }

        // Refs không có lookup — giữ nguyên qua hidden để sửa không xoá.
        public string? OrderRef { get; set; }
        public string? ProviderRef { get; set; }
        public string? AssigneeRef { get; set; }
    }

    public static string TripTypeLabel(int t) => t switch { 1 => "Khứ hồi", _ => "Một chiều" };

    public static string StatusLabel(int s) => s switch { 1 => "Đã duyệt", 2 => "Không duyệt", _ => "Tạo mới" };
    public static string StatusColor(int s) => s switch { 1 => "success", 2 => "danger", _ => "secondary" };

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateFlightTicketIndividualDto(
                Input.Code, Input.TicketCode, Input.Pnr, Input.CustomerName, Input.OrderRef, Input.ProviderRef,
                Input.TripType, Input.Route, Input.DepartDate, Input.ReturnDate,
                Input.SellAmount, Input.ReceivedAmount, Input.TotalCost, Input.PaidAmount,
                Input.PaymentDueDate, Input.Status, Input.AssigneeRef, Input.Note));
        }
        else
        {
            await _svc.CreateAsync(new CreateFlightTicketIndividualDto(
                Input.Code, Input.TicketCode, Input.Pnr, Input.CustomerName, Input.OrderRef, Input.ProviderRef,
                Input.TripType, Input.Route, Input.DepartDate, Input.ReturnDate,
                Input.SellAmount, Input.ReceivedAmount, Input.TotalCost, Input.PaidAmount,
                Input.PaymentDueDate, Input.Status, Input.AssigneeRef, Input.Note));
        }

        return new JsonResult(Result.Success("Đã lưu vé máy bay lẻ."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá vé máy bay lẻ.";
        return RedirectToPage();
    }
}
