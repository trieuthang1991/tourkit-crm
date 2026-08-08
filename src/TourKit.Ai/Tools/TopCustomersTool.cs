using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Top khách hàng theo doanh thu. Công cụ duy nhất trong bộ khởi điểm có THAM SỐ — schema của nó
/// được sinh tự động từ chữ ký hàm, nên đổi tham số là schema tự đổi theo.
/// </summary>
public sealed class TopCustomersTool : IAiTool
{
    private const int MaxTop = 50;

    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public TopCustomersTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_top_khach_hang",
            "Danh sách khách hàng đem lại doanh thu cao nhất, kèm số đã thu được của từng khách. " +
            "Dùng khi người hỏi muốn biết khách nào lớn nhất, top khách hàng, hoặc ai mua nhiều nhất.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.turnover.view";

    private async Task<AiToolResult> RunAsync(
        [Description("Số khách hàng muốn lấy, mặc định 10, tối đa 50.")] int top = 10)
    {
        // Model có thể gửi số vô lý (0, -1, 5000) — kẹp lại thay vì để truy vấn nhận số bậy.
        var take = Math.Clamp(top, 1, MaxTop);

        var rows = await _reports.GetTopCustomersAsync(take).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return new AiToolResult("Chưa có khách hàng nào phát sinh doanh thu.", null, "/bao-cao-tong-hop");
        }

        var sb = new StringBuilder("Top ").Append(rows.Count).Append(" khách hàng theo doanh thu (đơn vị: đồng):\n");
        var rank = 0;
        foreach (var r in rows)
        {
            sb.Append(++rank).Append(". ").Append(r.CustomerName)
              .Append(": doanh thu ").Append(AiFormat.Money(r.Revenue))
              .Append(", đã thu ").Append(AiFormat.Money(r.Received)).Append('\n');
        }

        var table = new AiTable(
            [new("Khách hàng"), new("Doanh thu", "money"), new("Đã thu", "money")],
            [.. rows.Select(r => (IReadOnlyList<object?>)[r.CustomerName, r.Revenue, r.Received])]);

        return new AiToolResult(sb.ToString(), table, "/bao-cao-tong-hop");
    }
}
