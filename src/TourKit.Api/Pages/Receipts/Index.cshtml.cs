using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Pages.Receipts;

// LIST read-only: IReceiptService.CreateAsync gắn theo orderId (FK) và là luồng duyệt phiếu
// (Create → Approve/Reject theo đơn), không có lookup enrich để chọn đơn/khách trên form
// → không dựng offcanvas create, chỉ hiển thị danh sách tổng (ListAllAsync).
[Authorize(Policy = "receipt.view")]
public class IndexModel : PageModel
{
    private readonly IReceiptService _svc;
    public IndexModel(IReceiptService svc) => _svc = svc;

    public IReadOnlyList<ReceiptListItemDto> Items { get; private set; } = [];

    // Trạng thái phiếu thu: 0 chờ duyệt · 1 đã duyệt · 2 từ chối.
    public static string StatusLabel(int s) => s switch
    {
        0 => "Chờ duyệt",
        1 => "Đã duyệt",
        2 => "Từ chối",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "success",
        2 => "danger",
        _ => "warning",
    };

    public async Task OnGetAsync() => Items = (await _svc.ListAllAsync(1, 1000)).Items;
}
