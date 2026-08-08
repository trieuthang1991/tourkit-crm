using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Doanh thu / thực thu / còn thiếu / lợi nhuận theo chi nhánh.</summary>
public sealed class TurnoverByBranchTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public TurnoverByBranchTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_doanh_thu_theo_chi_nhanh",
            "Doanh thu, thực thu, còn thiếu và lợi nhuận của TỪNG CHI NHÁNH, kèm số đơn hàng. " +
            "Dùng khi người hỏi muốn so sánh các chi nhánh, hoặc hỏi một chi nhánh cụ thể (Hà Nội, " +
            "Đà Nẵng, Sài Gòn…) thu được bao nhiêu.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.turnover.view";

    private async Task<AiToolResult> RunAsync()
    {
        var rows = (await _reports.GetTurnoverByBranchAsync().ConfigureAwait(false))
            .OrderByDescending(r => r.Turnover)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu doanh thu theo chi nhánh.", null, "/tong-quan");
        }

        var sb = new StringBuilder("Doanh thu theo chi nhánh (đơn vị: đồng):\n");
        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.BranchName)
              .Append(": ").Append(AiFormat.Count(r.OrderCount)).Append(" đơn")
              .Append(", doanh thu ").Append(AiFormat.Money(r.Turnover))
              .Append(", thực thu ").Append(AiFormat.Money(r.Received))
              .Append(", còn thiếu ").Append(AiFormat.Money(r.Outstanding))
              .Append(", lợi nhuận ").Append(AiFormat.Money(r.Profit)).Append('\n');
        }

        var table = new AiTable(
            [
                new("Chi nhánh"), new("Số đơn", "number"), new("Doanh thu", "money"),
                new("Thực thu", "money"), new("Còn thiếu", "money"), new("Chi phí", "money"),
                new("Lợi nhuận", "money"),
            ],
            [.. rows.Select(r => (IReadOnlyList<object?>)
                [r.BranchName, r.OrderCount, r.Turnover, r.Received, r.Outstanding, r.Cost, r.Profit])]);

        return new AiToolResult(sb.ToString(), table, "/tong-quan");
    }
}
