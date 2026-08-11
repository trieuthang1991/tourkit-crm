using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.CustomerDebtReport;

// Báo cáo read-only: công nợ khách theo đơn hàng (IReportService.GetOrderDebtAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.debt.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<OrderDebtRowDto> Items { get; private set; } = [];

    public decimal TotalAmount { get; private set; }
    public decimal TotalPaid { get; private set; }
    public decimal TotalOutstanding { get; private set; }

    /// <summary>
    /// Xuất CSV đúng thứ đang hiện trên màn. Báo cáo này không có nút xuất trong khi 15 màn khác đều
    /// có, nên bài kiểm thử xuất file tự bỏ qua thay vì đỏ — thiếu tính năng mà nhìn kết quả kiểm thử
    /// lại tưởng đã phủ.
    /// </summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        await OnGetAsync();   // dùng chung đường nạp, để file xuất không bao giờ lệch với màn hình

        var sb = new StringBuilder();
        sb.AppendLine("Mã đơn,Phải thu,Đã thu,Còn nợ");
        foreach (var x in Items)
        {
            static string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            static string S(decimal v) => v.ToString(CultureInfo.InvariantCulture);

            sb.Append(C(x.OrderCode)).Append(',')
              .Append(S(x.Total)).Append(',').Append(S(x.Paid)).Append(',').Append(S(x.Outstanding)).AppendLine();
        }

        // BOM UTF-8: thiếu nó là Excel mở ra tiếng Việt thành ký tự rác.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "cong-no-khach.csv");
    }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetOrderDebtAsync();
        TotalAmount = Items.Sum(x => x.Total);
        TotalPaid = Items.Sum(x => x.Paid);
        TotalOutstanding = Items.Sum(x => x.Outstanding);
    }
}
