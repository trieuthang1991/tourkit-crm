using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Công nợ phải thu theo đơn hàng. Chỉ trả về đơn CÒN NỢ và cắt ở 20 dòng nợ nhiều nhất — đưa cả trăm
/// dòng vào ngữ cảnh vừa tốn token vừa làm câu trả lời loãng. Tổng công nợ thì tính trên TOÀN BỘ đơn
/// còn nợ, không phải chỉ 20 dòng hiển thị.
/// </summary>
public sealed class OrderDebtTool : IAiTool
{
    private const int MaxRows = 20;

    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public OrderDebtTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_cong_no_phai_thu",
            "Công nợ KHÁCH HÀNG còn nợ công ty: tổng tiền đơn, đã thu, còn nợ, theo từng đơn hàng. " +
            $"Trả về tổng công nợ và {MaxRows} đơn nợ nhiều nhất. " +
            "Dùng khi người hỏi muốn biết đang bị nợ bao nhiêu, đơn nào nợ nhiều nhất, hoặc tổng phải thu.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.debt.view";

    private async Task<AiToolResult> RunAsync()
    {
        var all = (await _reports.GetOrderDebtAsync().ConfigureAwait(false))
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        if (all.Count == 0)
        {
            return new AiToolResult("Không có đơn hàng nào còn nợ.", null, "/cong-no-khach");
        }

        var rows = all.Take(MaxRows).ToList();
        var sb = new StringBuilder()
            .Append("Tổng công nợ phải thu: ").Append(AiFormat.Money(all.Sum(r => r.Outstanding)))
            .Append(" đồng, trên ").Append(AiFormat.Count(all.Count)).Append(" đơn còn nợ.\n");

        if (all.Count > MaxRows)
        {
            sb.Append("Dưới đây là ").Append(MaxRows).Append(" đơn nợ nhiều nhất:\n");
        }

        foreach (var r in rows)
        {
            sb.Append("- ").Append(r.OrderCode)
              .Append(": tổng ").Append(AiFormat.Money(r.Total))
              .Append(", đã thu ").Append(AiFormat.Money(r.Paid))
              .Append(", còn nợ ").Append(AiFormat.Money(r.Outstanding)).Append('\n');
        }

        var table = new AiTable(
            [new("Mã đơn"), new("Tổng tiền", "money"), new("Đã thu", "money"), new("Còn nợ", "money")],
            [.. rows.Select(r => (IReadOnlyList<object?>)[r.OrderCode, r.Total, r.Paid, r.Outstanding])]);

        return new AiToolResult(sb.ToString(), table, "/cong-no-khach");
    }
}
