using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.ProviderDebtReport;

// Báo cáo read-only: công nợ phải trả NCC + phân tuổi nợ (IReportService.GetProviderDebtAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.providerdebt.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<ProviderDebtRowDto> Items { get; private set; } = [];

    public decimal TotalCost { get; private set; }
    public decimal TotalPaid { get; private set; }
    public decimal TotalOutstanding { get; private set; }

    /// <summary>
    /// Tên NCC đang xem riêng, đến từ <c>/cong-no-ncc?providerId=…</c> — nút "Công nợ nhà cung cấp"
    /// trên màn Nhà cung cấp đi bằng đường này. Rỗng nghĩa là đang xem toàn bộ.
    /// </summary>
    public string? LocNccTen { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetProviderDebtAsync();

        // Báo cáo này vốn nạp trọn bảng để cộng tổng, nên lọc ngay trên kết quả đã có: không thêm
        // truy vấn nào. Trước đây providerId bị bỏ qua hoàn toàn — người dùng bấm "Công nợ" của đúng
        // một NCC lại nhận báo cáo của tất cả, và không có cách nào lọc vì màn này không có bộ lọc.
        if (Guid.TryParse(Request.Query["providerId"], out var nccId) && nccId != Guid.Empty)
        {
            var loc = Items.Where(x => x.ProviderId == nccId).ToList();

            // Không có dòng nào nghĩa là NCC đó chưa phát sinh công nợ. Vẫn lọc (hiện báo cáo rỗng
            // kèm tên NCC) thay vì âm thầm trả về toàn bộ — trả về toàn bộ là trả lời sai câu hỏi.
            LocNccTen = loc.Count > 0
                ? loc[0].ProviderName
                : Items.FirstOrDefault(x => x.ProviderId == nccId)?.ProviderName ?? "nhà cung cấp đã chọn";
            Items = loc;
        }

        TotalCost = Items.Sum(x => x.TotalCost);
        TotalPaid = Items.Sum(x => x.Paid);
        TotalOutstanding = Items.Sum(x => x.Outstanding);
    }
}
