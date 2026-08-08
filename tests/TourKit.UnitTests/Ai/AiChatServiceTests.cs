using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

/// <summary>Vòng lặp trợ lý — chạy hoàn toàn không cần LLM thật.</summary>
public class AiChatServiceTests
{
    private static AiChatService Service(IChatClient client, int maxRounds = 5, params IAiTool[] tools) =>
        new(client,
            new AiToolRegistry(tools),
            new AiChatSettings("model-test", 4000, maxRounds),
            NullLogger<AiChatService>.Instance);

    private static HashSet<string> NoPerms() => [];

    [Fact]
    public async Task Goi_cong_cu_roi_tra_loi_bang_luot_thu_hai()
    {
        var tool = new StubTool("doanh_thu", null, "Doanh thu 5 tỷ");
        var client = new FakeChatClient(
            FakeChatClient.CallsTool("t1", "doanh_thu"),
            FakeChatClient.Says("Doanh thu là 5 tỷ đồng."));

        var answer = await Service(client, 5, tool).AskAsync("doanh thu bao nhiêu?", NoPerms(), CancellationToken.None);

        Assert.Equal("Doanh thu là 5 tỷ đồng.", answer.Text);
        Assert.Equal(1, tool.Invocations);
        Assert.Equal(2, client.Calls);
        Assert.Single(answer.Blocks);
        Assert.Equal("/dich-den", answer.Blocks[0].LinkUrl);
        Assert.NotNull(answer.Blocks[0].Data);
    }

    /// <summary>Bộ lọc quyền phải áp TRƯỚC khi gửi đi — model không được thấy tên công cụ ngoài quyền.</summary>
    [Fact]
    public async Task Model_khong_thay_cong_cu_ngoai_quyen()
    {
        var client = new FakeChatClient(FakeChatClient.Says("ok"));
        var service = Service(client, 5,
            new StubTool("cong_khai", null),
            new StubTool("gia_von", "report.cost.view"));

        await service.AskAsync("giá vốn?", NoPerms(), CancellationToken.None);

        Assert.Equal(["cong_khai"], client.ToolNamesSeen[0]);
    }

    [Fact]
    public async Task Model_goi_cong_cu_khong_ton_tai_thi_bao_loi_ve_cho_model_chu_khong_nem()
    {
        var client = new FakeChatClient(
            FakeChatClient.CallsTool("t1", "khong_co_that"),
            FakeChatClient.Says("Tôi không tra được mục này."));

        var answer = await Service(client, 5, new StubTool("that", null))
            .AskAsync("hỏi linh tinh", NoPerms(), CancellationToken.None);

        Assert.Equal("Tôi không tra được mục này.", answer.Text);
        Assert.Contains(
            client.HistoriesSeen[1].SelectMany(m => m.Contents).OfType<FunctionResultContent>(),
            r => r.Result?.ToString()?.Contains("không tồn tại", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Cong_cu_nem_loi_thi_dua_thong_diep_ve_cho_model()
    {
        var client = new FakeChatClient(
            FakeChatClient.CallsTool("t1", "hong"),
            FakeChatClient.Says("Có lỗi khi tra số liệu."));

        var answer = await Service(client, 5, new StubTool("hong", null, throws: true))
            .AskAsync("hỏi", NoPerms(), CancellationToken.None);

        Assert.Equal("Có lỗi khi tra số liệu.", answer.Text);
        Assert.Contains(
            client.HistoriesSeen[1].SelectMany(m => m.Contents).OfType<FunctionResultContent>(),
            r => r.Result?.ToString()?.Contains("hỏng rồi", StringComparison.Ordinal) == true);
    }

    /// <summary>Một model gọi công cụ không ngừng có thể đốt hết hạn mức tháng trong một buổi.</summary>
    [Fact]
    public async Task Dung_o_vong_cuoi_neu_model_goi_cong_cu_khong_ngung()
    {
        var tool = new StubTool("lap", null);
        var loop = Enumerable.Range(0, 10).Select(_ => FakeChatClient.CallsTool("t", "lap")).ToArray();

        var answer = await Service(new FakeChatClient(loop), 5, tool)
            .AskAsync("lặp đi", NoPerms(), CancellationToken.None);

        Assert.Equal(5, tool.Invocations);
        Assert.Contains("chưa hoàn tất", answer.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Người dùng không có quyền với công cụ nào: thử thật với tài khoản điều hành cho thấy model nhận
    /// danh sách rỗng vẫn trả lời "chờ tôi lấy dữ liệu" rồi im. Phải chặn trước khi gọi.
    /// </summary>
    [Fact]
    public async Task Khong_co_cong_cu_nao_thi_khong_goi_model()
    {
        var client = new FakeChatClient(FakeChatClient.Says("Chờ tôi lấy dữ liệu nhé."));
        var service = Service(client, 5, new StubTool("gia_von", "report.cost.view"));

        var answer = await service.AskAsync("giá vốn?", NoPerms(), CancellationToken.None);

        Assert.Equal(0, client.Calls);
        Assert.Contains("chưa được cấp quyền", answer.Text, StringComparison.Ordinal);
        Assert.Empty(answer.Blocks);
    }

    [Fact]
    public async Task Model_tra_ve_rong_thi_van_co_cau_tra_loi_khong_de_man_hinh_trong()
    {
        var answer = await Service(new FakeChatClient(FakeChatClient.Says("")), 5, new StubTool("bat_ky", null))
            .AskAsync("hỏi", NoPerms(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(answer.Text));
    }

    [Fact]
    public async Task Nhieu_cong_cu_trong_mot_luot_deu_duoc_chay()
    {
        var a = new StubTool("a", null);
        var b = new StubTool("b", null);
        var both = new ChatMessage(ChatRole.Assistant, (IList<AIContent>)
        [
            new FunctionCallContent("1", "a", new Dictionary<string, object?>()),
            new FunctionCallContent("2", "b", new Dictionary<string, object?>()),
        ]);

        var answer = await Service(new FakeChatClient(both, FakeChatClient.Says("xong")), 5, a, b)
            .AskAsync("hỏi", NoPerms(), CancellationToken.None);

        Assert.Equal(1, a.Invocations);
        Assert.Equal(1, b.Invocations);
        Assert.Equal(2, answer.Blocks.Count);
    }
}
