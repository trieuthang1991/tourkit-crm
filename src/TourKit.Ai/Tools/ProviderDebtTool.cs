using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Công nợ phải TRẢ nhà cung cấp, kèm phân tuổi nợ. Đưa cả bốn nhóm tuổi vào phần văn bản vì "nợ quá
/// hạn bao lâu" mới là câu hỏi thật của kế toán, chứ không phải con số tổng.
/// </summary>
public sealed class ProviderDebtTool : IAiTool
{
    private const int MaxRows = 20;

    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public ProviderDebtTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_cong_no_phai_tra",
            "Công nợ công ty còn nợ NHÀ CUNG CẤP: tổng chi phí, đã trả, còn phải trả, kèm phân tuổi nợ " +
            "(0-30 ngày, 31-60, 61-90, trên 90 ngày). " +
            "Dùng khi người hỏi muốn biết đang nợ nhà cung cấp nào, nợ bao lâu rồi, hoặc tổng phải trả.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.providerdebt.view";

    private async Task<AiToolResult> RunAsync()
    {
        var all = (await _reports.GetProviderDebtAsync().ConfigureAwait(false))
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        if (all.Count == 0)
        {
            return new AiToolResult("Không còn nợ nhà cung cấp nào.", null, "/cong-no-ncc");
        }

        var rows = all.Take(MaxRows).ToList();
        var sb = new StringBuilder()
            .Append("Tổng công nợ phải trả: ").Append(AiFormat.Money(all.Sum(r => r.Outstanding)))
            .Append(" đồng, trên ").Append(AiFormat.Count(all.Count)).Append(" nhà cung cấp.\n")
            .Append("Trong đó quá hạn trên 90 ngày: ").Append(AiFormat.Money(all.Sum(r => r.D90Plus))).Append(" đồng.\n");

        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.ProviderName)
              .Append(": còn phải trả ").Append(AiFormat.Money(r.Outstanding))
              .Append(" (0-30 ngày ").Append(AiFormat.Money(r.Current))
              .Append(", 31-60 ").Append(AiFormat.Money(r.D30))
              .Append(", 61-90 ").Append(AiFormat.Money(r.D60))
              .Append(", trên 90 ").Append(AiFormat.Money(r.D90Plus)).Append(")\n");
        }

        var table = new AiTable(
            [
                new("Nhà cung cấp"), new("Tổng chi phí", "money"), new("Đã trả", "money"),
                new("Còn phải trả", "money"), new("0–30 ngày", "money"), new("31–60", "money"),
                new("61–90", "money"), new("Trên 90", "money"),
            ],
            [.. rows.Select(r => (IReadOnlyList<object?>)
                [r.ProviderName, r.TotalCost, r.Paid, r.Outstanding, r.Current, r.D30, r.D60, r.D90Plus])]);

        return new AiToolResult(sb.ToString(), table, "/cong-no-ncc");
    }
}
