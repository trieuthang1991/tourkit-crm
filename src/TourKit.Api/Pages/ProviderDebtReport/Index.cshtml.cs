using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>
    /// Xuất CSV đúng thứ đang hiện trên màn — kể cả khi đang lọc theo một NCC.
    ///
    /// Báo cáo này không có nút xuất trong khi 15 màn khác đều có, nên bài kiểm thử xuất file tự bỏ
    /// qua thay vì đỏ: thiếu tính năng mà nhìn vào kết quả kiểm thử lại tưởng đã phủ.
    /// </summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        await OnGetAsync();   // dùng chung đường nạp + lọc, để file xuất không bao giờ lệch với màn hình

        var sb = new StringBuilder();
        sb.AppendLine("Nhà cung cấp,Tổng chi phí,Đã chi,Còn phải trả,0-30 ngày,31-60,61-90,>90");
        foreach (var x in Items)
        {
            static string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            static string S(decimal v) => v.ToString(CultureInfo.InvariantCulture);

            sb.Append(C(x.ProviderName)).Append(',')
              .Append(S(x.TotalCost)).Append(',').Append(S(x.Paid)).Append(',').Append(S(x.Outstanding)).Append(',')
              .Append(S(x.Current)).Append(',').Append(S(x.D30)).Append(',').Append(S(x.D60)).Append(',')
              .Append(S(x.D90Plus)).AppendLine();
        }

        // BOM UTF-8: thiếu nó là Excel mở ra tên NCC tiếng Việt thành ký tự rác.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "cong-no-ncc.csv");
    }

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
