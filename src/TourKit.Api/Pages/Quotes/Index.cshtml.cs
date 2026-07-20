using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Quotes;

[Authorize(Policy = "quote.view")]
public class IndexModel : PageModel
{
    private readonly IQuoteService _svc;
    public IndexModel(IQuoteService svc) => _svc = svc;

    public IReadOnlyList<QuoteSummaryDto> Items { get; private set; } = [];
    public QuoteStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);

    [BindProperty(SupportsGet = true, Name = "type")] public int? QuoteType { get; set; }

    private static readonly string[] TypeLabels = ["Tour", "Combo", "GIT", "Landtour", "Booking phòng", "Dịch vụ lẻ", "Visa"];
    public string TypeLabel => QuoteType is int t && t >= 0 && t < TypeLabels.Length ? TypeLabels[t] : "Tất cả";

    public static string StatusLabel(int s) => s switch
    {
        1 => "Đã gửi",
        2 => "Chấp nhận",
        3 => "Từ chối",
        _ => "Nháp",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "info",
        2 => "success",
        3 => "secondary",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync(QuoteType);
        Items = (await _svc.ListAsync(1, 1000, new QuoteListFilter(QuoteType: QuoteType))).Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá báo giá.";
        return RedirectToPage();
    }
}
