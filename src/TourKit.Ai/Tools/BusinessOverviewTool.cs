using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Tổng quan kinh doanh: số đơn, doanh thu, đã thu, còn phải thu, chi phí, đã chi, còn phải trả, lãi gộp.
/// Gọi thẳng <see cref="IReportService"/> nên bộ lọc tenant của hàm đó tự áp — công cụ không tự viết truy vấn.
/// </summary>
public sealed class BusinessOverviewTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public BusinessOverviewTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_tong_quan_kinh_doanh",
            "Bức tranh tổng thể của cả công ty: tổng số đơn hàng, tổng doanh thu, đã thu được bao nhiêu, " +
            "còn phải thu của khách, tổng chi phí, đã chi cho nhà cung cấp, còn phải trả, và lãi gộp. " +
            "Dùng khi người hỏi muốn nắm tình hình chung, hỏi 'công ty đang thế nào', hoặc hỏi lãi/lỗ.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.dashboard.view";

    private async Task<AiToolResult> RunAsync()
    {
        var d = await _reports.GetDashboardAsync().ConfigureAwait(false);

        var text = new StringBuilder("Tổng quan kinh doanh (đơn vị: đồng):\n")
            .Append("- Số đơn hàng: ").Append(AiFormat.Count(d.OrderCount)).Append('\n')
            .Append("- Doanh thu: ").Append(AiFormat.Money(d.TotalRevenue)).Append('\n')
            .Append("- Đã thu của khách: ").Append(AiFormat.Money(d.TotalReceived))
            .Append(", ").Append(Balance(d.ReceivableOutstanding, "còn phải thu", "khách đã trả thừa")).Append('\n')
            .Append("- Tổng chi phí: ").Append(AiFormat.Money(d.TotalCost))
            .Append(", đã chi: ").Append(AiFormat.Money(d.TotalPaid))
            .Append(", ").Append(Balance(d.PayableOutstanding, "còn phải trả", "đã trả thừa so với chi phí đã ghi nhận")).Append('\n')
            .Append("- Lãi gộp: ").Append(AiFormat.Money(d.GrossProfit)).Append('\n')
            .ToString();

        // Một bản ghi tổng hợp thì bảng ngang không đọc được — trải thành cặp chỉ tiêu / giá trị.
        var table = new AiTable(
            [new("Chỉ tiêu"), new("Giá trị", "money")],
            [
                [(object?)"Số đơn hàng", d.OrderCount],
                [(object?)"Doanh thu", d.TotalRevenue],
                [(object?)"Đã thu của khách", d.TotalReceived],
                [(object?)"Còn phải thu", d.ReceivableOutstanding],
                [(object?)"Tổng chi phí", d.TotalCost],
                [(object?)"Đã chi", d.TotalPaid],
                [(object?)"Còn phải trả", d.PayableOutstanding],
                [(object?)"Lãi gộp", d.GrossProfit],
            ]);

        return new AiToolResult(text, table, "/tong-quan");
    }

    /// <summary>
    /// Số dư âm nói bằng lời, không in dấu trừ. "Còn phải trả: -1.827.614.000" đọc như lỗi hệ thống,
    /// trong khi nó chỉ có nghĩa là đã chi nhiều hơn phần chi phí đã ghi nhận — một tình trạng có
    /// thật và kế toán cần biết, nên phải nói ra chứ không phải giấu đi.
    /// </summary>
    private static string Balance(decimal value, string whenOwing, string whenOverpaid) =>
        value >= 0
            ? $"{whenOwing}: {AiFormat.Money(value)}"
            : $"{whenOverpaid}: {AiFormat.Money(-value)}";
}
