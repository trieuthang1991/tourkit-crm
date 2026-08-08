using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Thu chi theo loại tour, có tách phần hoàn/huỷ chỗ ra khỏi doanh thu gộp.</summary>
public sealed class MoneyByTourTypeTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public MoneyByTourTypeTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_thu_chi_theo_loai_tour",
            "Doanh thu, tiền hoàn/huỷ chỗ, doanh thu ròng, chi phí và lợi nhuận theo TỪNG LOẠI TOUR " +
            "(khách lẻ, khách đoàn…). Dùng khi người hỏi muốn so sánh các loại tour, hoặc hỏi loại tour " +
            "nào lãi nhất, hoặc hỏi về tiền hoàn huỷ.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.turnover.view";

    private async Task<AiToolResult> RunAsync()
    {
        var rows = (await _reports.GetMoneyByTourTypeAsync().ConfigureAwait(false))
            .OrderByDescending(r => r.NetRevenue)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu thu chi theo loại tour.", null, "/thu-chi-theo-tour");
        }

        var sb = new StringBuilder("Thu chi theo loại tour (đơn vị: đồng):\n");
        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.TourTypeName)
              .Append(": ").Append(AiFormat.Count(r.OrderCount)).Append(" đơn")
              .Append(", doanh thu gộp ").Append(AiFormat.Money(r.GrossRevenue))
              .Append(", hoàn huỷ ").Append(AiFormat.Money(r.Refund))
              .Append(", doanh thu ròng ").Append(AiFormat.Money(r.NetRevenue))
              .Append(", chi phí ").Append(AiFormat.Money(r.Cost))
              .Append(", lợi nhuận ").Append(AiFormat.Money(r.Profit)).Append('\n');
        }

        var table = new AiTable(
            [
                new("Loại tour"), new("Số đơn", "number"), new("Doanh thu gộp", "money"),
                new("Hoàn huỷ", "money"), new("Doanh thu ròng", "money"), new("Chi phí", "money"),
                new("Lợi nhuận", "money"),
            ],
            [.. rows.Select(r => (IReadOnlyList<object?>)
                [r.TourTypeName, r.OrderCount, r.GrossRevenue, r.Refund, r.NetRevenue, r.Cost, r.Profit])]);

        return new AiToolResult(sb.ToString(), table, "/thu-chi-theo-tour");
    }
}
