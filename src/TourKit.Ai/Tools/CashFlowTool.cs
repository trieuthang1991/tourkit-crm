using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Dòng tiền theo phương thức thanh toán: thu vào, chi ra, ròng.</summary>
public sealed class CashFlowTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public CashFlowTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_dong_tien",
            "Dòng tiền theo từng phương thức thanh toán (tiền mặt, chuyển khoản…): thu vào, chi ra và " +
            "chênh lệch ròng. Dùng khi người hỏi muốn biết tiền vào ra thế nào, thu chi qua kênh nào nhiều nhất.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.cashflow.view";

    private async Task<AiToolResult> RunAsync()
    {
        var rows = (await _reports.GetCashFlowAsync().ConfigureAwait(false))
            .OrderByDescending(r => r.Inflow)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult("Chưa có giao dịch thu chi nào.", null, "/dong-tien");
        }

        var sb = new StringBuilder()
            .Append("Dòng tiền (đơn vị: đồng). Tổng thu ").Append(AiFormat.Money(rows.Sum(r => r.Inflow)))
            .Append(", tổng chi ").Append(AiFormat.Money(rows.Sum(r => r.Outflow)))
            .Append(", ròng ").Append(AiFormat.Money(rows.Sum(r => r.Net))).Append(".\n");

        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.PaymentMethod)
              .Append(": thu ").Append(AiFormat.Money(r.Inflow))
              .Append(", chi ").Append(AiFormat.Money(r.Outflow))
              .Append(", ròng ").Append(AiFormat.Money(r.Net)).Append('\n');
        }

        var table = new AiTable(
            [new("Phương thức"), new("Thu vào", "money"), new("Chi ra", "money"), new("Ròng", "money")],
            [.. rows.Select(r => (IReadOnlyList<object?>)[r.PaymentMethod, r.Inflow, r.Outflow, r.Net])]);

        return new AiToolResult(sb.ToString(), table, "/dong-tien");
    }
}
