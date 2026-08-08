using System.Text;
using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Phễu kinh doanh: báo giá → được chấp nhận → chuyển thành đơn → thu được tiền.</summary>
public sealed class KpiSummaryTool : IAiTool
{
    private readonly IReportService _reports;

    /// <summary>Khởi tạo và dựng khai báo gửi cho model.</summary>
    public KpiSummaryTool(IReportService reports)
    {
        _reports = reports;
        Function = AIFunctionFactory.Create(
            RunAsync,
            "bao_cao_phieu_kinh_doanh",
            "Phễu kinh doanh: số báo giá đã gửi, bao nhiêu được khách chấp nhận, bao nhiêu chuyển thành " +
            "đơn hàng, tỉ lệ chốt, giá trị đơn trung bình và tỉ lệ thu được tiền. " +
            "Dùng khi người hỏi về hiệu quả bán hàng, tỉ lệ chốt deal, hay giá trị đơn trung bình.");
    }

    /// <inheritdoc/>
    public AIFunction Function { get; }

    /// <inheritdoc/>
    public string? RequiredPermission => "report.dashboard.view";

    private async Task<AiToolResult> RunAsync()
    {
        var k = await _reports.GetKpiSummaryAsync().ConfigureAwait(false);

        var text = new StringBuilder("Phễu kinh doanh:\n")
            .Append("- Báo giá đã gửi: ").Append(AiFormat.Count(k.QuoteCount))
            .Append("; khách chấp nhận: ").Append(AiFormat.Count(k.QuoteAcceptedCount))
            .Append(" (tỉ lệ ").Append(AiFormat.Percent(k.AcceptanceRate)).Append(")\n")
            .Append("- Chuyển thành đơn: ").Append(AiFormat.Count(k.QuoteConvertedCount))
            .Append(" (tỉ lệ ").Append(AiFormat.Percent(k.ConversionRate)).Append(")\n")
            .Append("- Tổng đơn: ").Append(AiFormat.Count(k.OrderCount))
            .Append("; doanh thu ").Append(AiFormat.Money(k.TotalRevenue))
            .Append(" đồng; giá trị đơn trung bình ").Append(AiFormat.Money(k.AvgOrderValue)).Append(" đồng\n")
            .Append("- Đã thu: ").Append(AiFormat.Money(k.TotalReceived))
            .Append(" đồng (tỉ lệ thu ").Append(AiFormat.Percent(k.CollectionRate)).Append(")\n")
            .ToString();

        var table = new AiTable(
            [new("Chỉ tiêu"), new("Giá trị")],
            [
                [(object?)"Báo giá đã gửi", AiFormat.Count(k.QuoteCount)],
                [(object?)"Khách chấp nhận", $"{AiFormat.Count(k.QuoteAcceptedCount)} ({AiFormat.Percent(k.AcceptanceRate)})"],
                [(object?)"Chuyển thành đơn", $"{AiFormat.Count(k.QuoteConvertedCount)} ({AiFormat.Percent(k.ConversionRate)})"],
                [(object?)"Tổng đơn", AiFormat.Count(k.OrderCount)],
                [(object?)"Doanh thu", AiFormat.Money(k.TotalRevenue)],
                [(object?)"Giá trị đơn trung bình", AiFormat.Money(k.AvgOrderValue)],
                [(object?)"Đã thu", $"{AiFormat.Money(k.TotalReceived)} ({AiFormat.Percent(k.CollectionRate)})"],
            ]);

        return new AiToolResult(text, table, "/bao-cao-tong-hop");
    }
}
