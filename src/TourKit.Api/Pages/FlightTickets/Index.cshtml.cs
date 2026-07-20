using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Api.Pages.FlightTickets;

[Authorize(Policy = "ticketfund.view")]
public class IndexModel : PageModel
{
    private readonly IFlightTicketService _svc;
    public IndexModel(IFlightTicketService svc) => _svc = svc;

    public IReadOnlyList<FlightTicketDto> Items { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập PNR")] public string Pnr { get; set; } = "";
        public string? TourType { get; set; }
        public int Days { get; set; }
        public DateTimeOffset? DepartureDate { get; set; }
        public int Quantity { get; set; }
        public int UsedQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReservedAmount { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }

        // Passthrough (không có lookup/không dựng editor chặng): giữ nguyên khi sửa để không xoá dữ liệu.
        public string? MarketRef { get; set; }
        public string? ProviderRef { get; set; }
        public string? OrderRef { get; set; }
        public string? SegmentsJson { get; set; }
    }

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        var segments = FlightItinerary.Parse(Input.SegmentsJson).Segments;

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateFlightTicketDto(
                Input.Pnr, Input.MarketRef, Input.ProviderRef, Input.TourType, Input.Days, Input.DepartureDate,
                Input.Quantity, Input.UsedQuantity, Input.OrderRef, Input.TotalCost, Input.PaidAmount, Input.ReservedAmount,
                Input.Status, Input.Note, segments));
        }
        else
        {
            await _svc.CreateAsync(new CreateFlightTicketDto(
                Input.Pnr, Input.MarketRef, Input.ProviderRef, Input.TourType, Input.Days, Input.DepartureDate,
                Input.Quantity, Input.TotalCost, Input.ReservedAmount, Input.Note, segments));
        }

        return new JsonResult(Result.Success("Đã lưu vé máy bay đoàn."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá vé máy bay đoàn.";
        return RedirectToPage();
    }
}
