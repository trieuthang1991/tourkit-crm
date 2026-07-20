using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Pages.TicketFunds;

// LIST read-only: Quỹ vé ứng (legacy TicketFund) — ITicketFundService.ListAsync + GetStatsAsync.
// KHÁC màn FlightTickets (IFlightTicketService, vé máy bay đoàn theo PNR): đây là quỹ vé ứng gắn theo ĐƠN + NCC.
// CreateTicketFundDto cần OrderId/ProviderId/ProviderServiceId (FK, không lookup enrich) → chỉ hiển thị danh sách.
[Authorize(Policy = "ticketfund.view")]
public class IndexModel : PageModel
{
    private readonly ITicketFundService _svc;
    public IndexModel(ITicketFundService svc) => _svc = svc;

    public IReadOnlyList<TicketFundDto> Items { get; private set; } = [];
    public TicketFundStatsDto Stats { get; private set; } = new(0, 0, 0);

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Items = (await _svc.ListAsync(1, 1000)).Items;
    }
}
