using TourKit.Ai;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Bài kiểm tra AN TOÀN quan trọng nhất của trợ lý: model chỉ được thấy công cụ đúng quyền của người
/// đang hỏi. Không có LLM tham gia nên kết quả tất định 100%.
/// </summary>
public class AiToolRegistryTests
{
    private static AiToolRegistry Registry() => new(
    [
        new StubTool("cong_khai", null),
        new StubTool("doanh_thu", "report.turnover.view"),
        new StubTool("cong_no", "report.debt.view"),
    ]);

    [Fact]
    public void Chi_tra_ve_cong_cu_dung_quyen()
    {
        var allowed = Registry().For(new HashSet<string> { "report.turnover.view" });

        Assert.Equal(["cong_khai", "doanh_thu"], allowed.Select(t => t.Function.Name).Order());
    }

    [Fact]
    public void Khong_co_quyen_nao_thi_chi_thay_cong_cu_cong_khai()
    {
        var allowed = Registry().For(new HashSet<string>());

        Assert.Equal(["cong_khai"], allowed.Select(t => t.Function.Name));
    }

    /// <summary>Model bịa tên một công cụ nó không được thấy — phải trả null, không phải công cụ thật.</summary>
    [Fact]
    public void Find_khong_tra_ve_cong_cu_ngoai_danh_sach_da_loc()
    {
        var allowed = Registry().For(new HashSet<string>());

        Assert.Null(AiToolRegistry.Find(allowed, "cong_no"));
    }

    [Fact]
    public void Find_tra_ve_cong_cu_khi_nam_trong_danh_sach()
    {
        var allowed = Registry().For(new HashSet<string> { "report.debt.view" });

        Assert.Equal("cong_no", AiToolRegistry.Find(allowed, "cong_no")?.Function.Name);
    }

    /// <summary>Có đủ quyền thì thấy hết — chặn kiểu lọc quá tay làm trợ lý vô dụng.</summary>
    [Fact]
    public void Du_quyen_thi_thay_toan_bo()
    {
        var allowed = Registry().For(new HashSet<string> { "report.turnover.view", "report.debt.view" });

        Assert.Equal(3, allowed.Count);
    }
}
