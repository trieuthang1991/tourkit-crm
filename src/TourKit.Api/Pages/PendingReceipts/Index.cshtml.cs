using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Pages.PendingReceipts;

// LIST read-only: Phiếu thu CHỜ DUYỆT — IReceiptService.ListAllAsync với ReceiptListFilter(Status = 0 chờ duyệt).
// Duyệt/Từ chối là luồng theo đơn (ApproveAsync/RejectAsync theo receiptId) → màn này chỉ hiển thị hàng chờ.
[Authorize(Policy = "receipt.view")]
public class IndexModel : PageModel
{
    private const int StatusPending = 0; // 0 chờ duyệt · 1 đã duyệt · 2 từ chối

    private readonly IReceiptService _svc;
    public IndexModel(IReceiptService svc) => _svc = svc;

    public IReadOnlyList<ReceiptListItemDto> Items { get; private set; } = [];
    public ReceiptStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public decimal PendingAmount { get; private set; }

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Items = (await _svc.ListAllAsync(1, 1000, new ReceiptListFilter(Status: StatusPending))).Items;
        PendingAmount = Items.Sum(x => x.Amount);
    }
}
