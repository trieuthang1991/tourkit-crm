using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Doanh thu / chi phí / lợi nhuận theo phòng ban của nhân viên phụ trách đơn.</summary>
public sealed class TurnoverByDepartmentTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public TurnoverByDepartmentTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_doanh_thu_theo_phong_ban",
            "Doanh thu, chi phí và lợi nhuận theo TỪNG PHÒNG BAN, kèm số đơn hàng. " +
            "Dùng khi người hỏi muốn so sánh các phòng ban hoặc hỏi một phòng ban cụ thể làm được bao nhiêu.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.turnover.view";

    private async Task<AiToolResult> RunAsync()
    {
        var rows = (await _reports.GetTurnoverByDepartmentAsync().ConfigureAwait(false))
            .OrderByDescending(r => r.Turnover)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu doanh thu theo phòng ban.", null, "/tong-quan");
        }

        var sb = new StringBuilder("Doanh thu theo phòng ban (đơn vị: đồng):\n");
        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.DepartmentName)
              .Append(": ").Append(AiFormat.Count(r.OrderCount)).Append(" đơn")
              .Append(", doanh thu ").Append(AiFormat.Money(r.Turnover))
              .Append(", chi phí ").Append(AiFormat.Money(r.Cost))
              .Append(", lợi nhuận ").Append(AiFormat.Money(r.Profit)).Append('\n');
        }

        var table = new AiTable(
            [new("Phòng ban"), new("Số đơn", "number"), new("Doanh thu", "money"), new("Chi phí", "money"), new("Lợi nhuận", "money")],
            [.. rows.Select(r => (IReadOnlyList<object?>)[r.DepartmentName, r.OrderCount, r.Turnover, r.Cost, r.Profit])]);

        return new AiToolResult(sb.ToString(), table, "/tong-quan");
    }
}
