using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;
using TourKit.Ai.Tools;
using TourKit.Application.Reports.Dtos;

namespace TourKit.UnitTests.Ai;

/// <summary>Nội dung công cụ trả về — thứ model đọc để trả lời, nên sai ở đây là sai câu trả lời.</summary>
public class ReportToolsTests
{
    private static async Task<AiToolResult> Run(IAiTool tool)
    {
        var raw = await tool.Function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);
        return System.Text.Json.JsonSerializer.Deserialize<AiToolResult>(
            (System.Text.Json.JsonElement)raw!, AIJsonUtilities.DefaultOptions)!;
    }

    /// <summary>
    /// Số dư âm phải nói bằng lời. Dữ liệu thật có 12,27 tỷ đã chi trên 10,44 tỷ chi phí ghi nhận,
    /// và "còn phải trả: -1.827.614.000" đọc như lỗi hệ thống chứ không như một tình trạng nghiệp vụ.
    /// </summary>
    [Fact]
    public async Task Tong_quan_khong_in_dau_tru_cho_so_du_am()
    {
        var svc = new FakeReportService
        {
            Dashboard = new DashboardSummaryDto(
                OrderCount: 10,
                TotalRevenue: 1000m, TotalReceived: 1200m, ReceivableOutstanding: -200m,
                TotalCost: 500m, TotalPaid: 700m, PayableOutstanding: -200m,
                GrossProfit: 500m),
        };

        var result = await Run(new BusinessOverviewTool(svc));

        Assert.DoesNotContain("-200", result.Text, StringComparison.Ordinal);
        Assert.Contains("khách đã trả thừa", result.Text, StringComparison.Ordinal);
        Assert.Contains("đã trả thừa so với chi phí", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tong_quan_so_duong_van_goi_la_con_phai_thu()
    {
        var svc = new FakeReportService
        {
            Dashboard = new DashboardSummaryDto(10, 1000m, 400m, 600m, 500m, 200m, 300m, 500m),
        };

        var result = await Run(new BusinessOverviewTool(svc));

        Assert.Contains("còn phải thu: 600", result.Text, StringComparison.Ordinal);
        Assert.Contains("còn phải trả: 300", result.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Công cụ này cộng RIÊNG các đơn còn nợ, còn báo cáo tổng quan bù trừ cả đơn đã thu thừa — trên
    /// dữ liệu thật hai số chênh 4,5 tỷ. Không nói rõ phạm vi thì trợ lý gọi cả hai công cụ trong một
    /// câu trả lời sẽ đưa ra hai con số trông như mâu thuẫn.
    /// </summary>
    [Fact]
    public async Task Cong_no_noi_ro_pham_vi_de_khong_mau_thuan_voi_tong_quan()
    {
        var svc = new FakeReportService
        {
            Debts = [new(Guid.NewGuid(), "DH0001", Guid.NewGuid(), 500m, 100m, 400m)],
        };

        var result = await Run(new OrderDebtTool(svc));

        Assert.Contains("RIÊNG các đơn còn nợ", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tổng công nợ phải thu", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cong_no_sap_xep_giam_dan_va_cat_o_20_dong()
    {
        var svc = new FakeReportService
        {
            Debts = [.. Enumerable.Range(1, 50).Select(i =>
                new OrderDebtRowDto(Guid.NewGuid(), $"DH{i:0000}", Guid.NewGuid(), 1000m, 0m, 1000m - i))],
        };

        var result = await Run(new OrderDebtTool(svc));

        Assert.Contains("DH0001", result.Text, StringComparison.Ordinal);
        Assert.Contains("DH0020", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("DH0021", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Khong_co_du_lieu_thi_noi_ro_chu_khong_tra_bang_rong()
    {
        var result = await Run(new OrderDebtTool(new FakeReportService()));

        Assert.Contains("Không có đơn hàng nào còn nợ", result.Text, StringComparison.Ordinal);
        Assert.Null(result.Data);
    }

    /// <summary>Model có thể gửi số vô lý — công cụ phải kẹp lại chứ không đẩy xuống truy vấn.</summary>
    [Fact]
    public async Task Top_khach_hang_kep_tham_so_vo_ly()
    {
        var svc = new FakeReportService
        {
            TopCustomers = [.. Enumerable.Range(1, 80).Select(i =>
                new TopCustomerRowDto(Guid.NewGuid(), $"Khách {i}", 100m - i, 0m))],
        };

        var tool = new TopCustomersTool(svc);

        var raw = await tool.Function.InvokeAsync(new AIFunctionArguments { ["top"] = 5000 }, CancellationToken.None);
        var result = System.Text.Json.JsonSerializer.Deserialize<AiToolResult>(
            (System.Text.Json.JsonElement)raw!, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal(50, svc.LastTopRequested);
        Assert.Contains("Top 50", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Moi_cong_cu_deu_khai_ma_quyen_va_mo_ta_tieng_viet()
    {
        var svc = new FakeReportService();

        Assert.Equal("report.dashboard.view", new BusinessOverviewTool(svc).RequiredPermission);
        Assert.Equal("report.debt.view", new OrderDebtTool(svc).RequiredPermission);
        Assert.Equal("report.turnover.view", new TurnoverByBranchTool(svc).RequiredPermission);
        Assert.Contains("chi nhánh", new TurnoverByBranchTool(svc).Function.Description, StringComparison.Ordinal);
    }
}
