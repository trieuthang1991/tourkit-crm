using Microsoft.Extensions.DependencyInjection;
using TourKit.Application.Ai;
using TourKit.Tests.Support;

namespace TourKit.Tests.Ai;

/// <summary>
/// Kho kết quả AI. Điều đáng kiểm không phải "có ghi được không" mà là GIỮ ĐƯỢC LỊCH SỬ: mỗi lần
/// chấm là một dòng mới, và màn hình luôn đọc đúng dòng mới nhất. Ghi đè thì mất khả năng so điểm
/// theo thời gian — mà đó mới là lý do lưu.
/// </summary>
public class AiInsightStoreTests
{
    private static SaveAiInsightDto ChamDiem(string id, int diem, string band = "Ấm") =>
        new("Customer", id, "Review", Guid.NewGuid(), Score: diem, Band: band,
            Summary: $"Nhận định {diem}", DetailJson: """{"criteria":[],"risks":[],"nextActions":[]}""");

    [Fact]
    public async Task Luu_xong_thi_doc_lai_duoc_dung_noi_dung()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiInsightStore>();

        var id = Guid.NewGuid().ToString();
        await store.SaveAsync(ChamDiem(id, 72, "Nóng"));

        var latest = await store.LatestAsync("Customer", id);
        var row = Assert.Single(latest);
        Assert.Equal("Review", row.Kind);
        Assert.Equal(72, row.Score);
        Assert.Equal("Nóng", row.Band);
        Assert.Equal("Nhận định 72", row.Summary);
    }

    /// <summary>
    /// Chấm lần hai KHÔNG được đè lần một. Đây chính là điều phân biệt "lưu lịch sử" với "lưu kết
    /// quả": mất dòng cũ là mất luôn câu trả lời cho "khách này đang ấm lên hay nguội đi".
    /// </summary>
    [Fact]
    public async Task Cham_lai_thi_them_dong_moi_chu_khong_de_dong_cu()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiInsightStore>();

        var id = Guid.NewGuid().ToString();
        await store.SaveAsync(ChamDiem(id, 40));
        await Task.Delay(15);   // để hai mốc thời gian tách nhau, đủ cho phép sắp xếp
        await store.SaveAsync(ChamDiem(id, 85));

        var lichSu = await store.HistoryAsync("Customer", id, "Review");
        Assert.Equal(2, lichSu.Count);

        // Mới nhất đứng trước: màn hình đọc phần tử đầu để hiện, không phải tự sắp lại.
        Assert.Equal(85, lichSu[0].Score);
        Assert.Equal(40, lichSu[1].Score);

        var latest = await store.LatestAsync("Customer", id);
        Assert.Equal(85, Assert.Single(latest).Score);
    }

    /// <summary>Mỗi loại việc giữ dòng mới nhất của riêng nó — chấm điểm không đè mất bản tóm tắt.</summary>
    [Fact]
    public async Task Moi_loai_viec_giu_dong_moi_nhat_cua_rieng_no()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiInsightStore>();

        var id = Guid.NewGuid().ToString();
        var ai = Guid.NewGuid();
        await store.SaveAsync(ChamDiem(id, 55));
        await store.SaveAsync(new SaveAiInsightDto("Customer", id, "Summary", ai, Text: "Diễn biến gần đây…"));
        await store.SaveAsync(new SaveAiInsightDto("Customer", id, "Draft", ai, Text: "Chào anh Sơn…"));

        var latest = await store.LatestAsync("Customer", id);
        Assert.Equal(3, latest.Count);
        Assert.Equal(55, latest.Single(x => x.Kind == "Review").Score);
        Assert.Equal("Diễn biến gần đây…", latest.Single(x => x.Kind == "Summary").Text);
        Assert.Equal("Chào anh Sơn…", latest.Single(x => x.Kind == "Draft").Text);
    }

    /// <summary>Kết quả của bản ghi này không được rò sang bản ghi khác.</summary>
    [Fact]
    public async Task Khong_lay_nham_ket_qua_cua_ban_ghi_khac()
    {
        using var factory = new AuthTestFactory();
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiInsightStore>();

        var a = Guid.NewGuid().ToString();
        var b = Guid.NewGuid().ToString();
        await store.SaveAsync(ChamDiem(a, 90));

        Assert.Empty(await store.LatestAsync("Customer", b));
        Assert.Empty(await store.HistoryAsync("Customer", b, "Review"));

        // Cùng khoá nhưng khác loại entity cũng phải tách bạch.
        Assert.Empty(await store.LatestAsync("Lead", a));
    }
}
