using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Invoices;

// LIST read-only: IInvoiceService có đủ CRUD nhưng Create/Update yêu cầu một tập DÒNG hoá đơn
// động (Lines[] — hoá đơn không thể tạo rỗng, subtotal/VAT/total tính từ dòng). Khuôn offcanvas
// phẳng (WorkTasks) không dựng được collection dòng động và Pages/ chưa có tiền lệ; để tránh bịa
// và bug khi không build/verify được, làm màn danh sách read-only (ListAsync trả InvoiceSummaryDto).
[Authorize(Policy = "invoice.view")]
public class IndexModel : PageModel
{
    private readonly IInvoiceService _svc;
    public IndexModel(IInvoiceService svc) => _svc = svc;

    public IReadOnlyList<InvoiceSummaryDto> Items { get; private set; } = [];

    // Trạng thái hoá đơn: 0 nháp · 1 đã phát hành · 2 đã huỷ.
    public static string StatusLabel(int s) => s switch
    {
        0 => "Nháp",
        1 => "Đã phát hành",
        2 => "Đã huỷ",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "success",
        2 => "danger",
        _ => "secondary",
    };

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;
}
