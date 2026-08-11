using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Ai;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Bóc bảng giá từ tài liệu báo giá bằng AI (bước 2 của IMPORT).
///
/// Model chỉ có MỘT việc: khớp chữ trong tài liệu vào tên cột cho sẵn. Vì đầu ra của nó không đáng
/// tin theo định nghĩa, phần đọc câu trả lời phải chịu được mọi kiểu trả về lệch chuẩn — đó là thứ
/// bài này kiểm, chứ không kiểm model đoán đúng hay sai.
/// </summary>
public class AiTableExtractTests
{
    private static readonly string[] Cot = ["Tên gói giá", "Giá hợp đồng", "Loại ngày"];

    private static AiTableExtractService Svc(string traLoi) =>
        new(new FakeChatClient(new ChatMessage(ChatRole.Assistant, traLoi)),
            new AiChatSettings("m", 1000, 1, null),
            NullLogger<AiTableExtractService>.Instance);

    private static Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> Chay(string traLoi) =>
        Svc(traLoi).ExtractAsync("nội dung tài liệu", Cot, CancellationToken.None);

    [Fact]
    public async Task Doc_duoc_JSON_thuan()
    {
        var rows = await Chay("""{"rows":[{"Tên gói giá":"Phòng Deluxe","Giá hợp đồng":"1.200.000","Loại ngày":"Ngày thường"}]}""");

        var r = Assert.Single(rows);
        Assert.Equal("Phòng Deluxe", r["Tên gói giá"]);
        Assert.Equal("1.200.000", r["Giá hợp đồng"]);
    }

    /// <summary>Đã dặn trả JSON thuần nhưng model vẫn hay kèm lời dẫn hoặc bọc trong khối mã.</summary>
    [Theory]
    [InlineData("Đây là bảng bạn cần:\n{\"rows\":[{\"Tên gói giá\":\"A\"}]}")]
    [InlineData("```json\n{\"rows\":[{\"Tên gói giá\":\"A\"}]}\n```")]
    [InlineData("{\"rows\":[{\"Tên gói giá\":\"A\"}]}\nHy vọng giúp được bạn.")]
    public async Task Doc_duoc_ca_khi_model_kem_loi_dan(string traLoi)
    {
        var rows = await Chay(traLoi);
        Assert.Equal("A", Assert.Single(rows)["Tên gói giá"]);
    }

    /// <summary>
    /// Model hay trả SỐ thay vì chuỗi cho ô giá dù prompt dặn giữ nguyên văn. Phải ép về chuỗi, không
    /// được ném lỗi — nếu không thì cả tài liệu hỏng chỉ vì một ô model tự ý đổi kiểu.
    /// </summary>
    [Fact]
    public async Task So_tra_ve_dang_number_van_doc_duoc_thanh_chuoi()
    {
        var rows = await Chay("""{"rows":[{"Tên gói giá":"A","Giá hợp đồng":1200000}]}""");

        Assert.Equal("1200000", Assert.Single(rows)["Giá hợp đồng"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("xin lỗi, tôi không đọc được tài liệu này")]
    [InlineData("{ khong-phai-json")]
    [InlineData("""{"khong-co-rows":1}""")]
    public async Task Tra_ve_khong_doc_duoc_thi_ra_danh_sach_rong_chu_khong_nem_loi(string traLoi)
    {
        Assert.Empty(await Chay(traLoi));
    }

    /// <summary>Tài liệu không phải bảng giá — prompt dặn trả mảng rỗng, và đó là kết quả hợp lệ.</summary>
    [Fact]
    public async Task Mang_rong_la_ket_qua_hop_le()
    {
        Assert.Empty(await Chay("""{"rows":[]}"""));
    }

    /// <summary>Danh sách cột phải nằm trong prompt, nếu không model không biết phải điền vào đâu.</summary>
    [Fact]
    public async Task Prompt_phai_liet_ke_du_ten_cot()
    {
        var fake = new FakeChatClient(new ChatMessage(ChatRole.Assistant, """{"rows":[]}"""));
        var svc = new AiTableExtractService(fake, new AiChatSettings("m", 1000, 1, null),
            NullLogger<AiTableExtractService>.Instance);

        await svc.ExtractAsync("tài liệu", Cot, CancellationToken.None);

        var prompt = fake.HistoriesSeen[0][0].Text;
        foreach (var c in Cot)
        {
            Assert.Contains(c, prompt, StringComparison.Ordinal);
        }
    }
}
