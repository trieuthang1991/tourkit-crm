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
            "Danh sách ĐƠN HÀNG còn nợ tiền khách: tổng tiền đơn, đã thu, còn nợ. " +
            $"Trả về {MaxRows} đơn nợ nhiều nhất và tổng của riêng các đơn còn nợ. " +
            "Dùng khi người hỏi đơn nào đang nợ, ai nợ nhiều nhất. " +
            "LƯU Ý: con số ở đây CHỈ cộng các đơn còn nợ, KHÁC với 'còn phải thu' của báo cáo tổng " +
            "quan (số đó bù trừ cả các đơn đã thu thừa nên luôn nhỏ hơn hoặc bằng). Nếu dùng cả hai " +
            "trong một câu trả lời thì phải nói rõ đó là hai cách tính khác nhau, đừng để người đọc " +
            "tưởng số liệu mâu thuẫn.");
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

        // Gọi đúng tên phạm vi của con số này. Trước đây gọi là "tổng công nợ phải thu" — trùng tên
        // với chỉ tiêu của báo cáo tổng quan nhưng tính khác (bên kia bù trừ cả đơn đã thu thừa), nên
        // khi trợ lý gọi cả hai công cụ trong một câu trả lời thì hai con số trông như mâu thuẫn.
        var sb = new StringBuilder()
            .Append(AiFormat.Count(all.Count)).Append(" đơn đang còn nợ, cộng lại ")
            .Append(AiFormat.Money(all.Sum(r => r.Outstanding))).Append(" đồng.\n")
            .Append("(Đây là tổng của RIÊNG các đơn còn nợ. Chỉ tiêu \"còn phải thu\" ở báo cáo tổng quan ")
            .Append("tính bù trừ cả các đơn đã thu thừa nên sẽ nhỏ hơn — hai cách tính khác nhau, không phải sai lệch.)\n");

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
