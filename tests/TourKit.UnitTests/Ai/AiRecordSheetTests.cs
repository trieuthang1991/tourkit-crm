using TourKit.Api.Ai;
using TourKit.Application.Collaboration;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Hồ sơ gửi cho AI. Đây là TOÀN BỘ những gì model nhìn thấy — thiếu một mảng dữ liệu thì mọi tính
/// năng (chấm điểm, tóm tắt, soạn tin) đều trả lời hụt, và triệu chứng là "AI trả lời chung chung"
/// chứ không phải một lỗi.
/// </summary>
public class AiRecordSheetTests
{
    private static readonly Guid Id = Guid.NewGuid();

    private static AiRecordSheet Sheet(
        LeadDto? lead = null, CustomerDto? customer = null, EntityCommentDto[]? comments = null) =>
        new(new FakeLeadService(lead), new FakeCustomerService(customer), new FakeCommentService(comments ?? []));

    private static LeadDto Lead() =>
        new(Id, "Trần Văn A", "0900000001", null, "Facebook", LeadStatus.Contacted, null, null);

    private static CustomerDto Customer() =>
        new(Id, "KH001", "Lý Kim Quân", "0900000002", 1, "Hotline", null, 1_500_000m,
            null, null, null, null, null, null, null,
            null, "Cần Thơ", "Khách lẻ", "Đi Đà Nẵng", null, null,
            null, null, null, null, null, "Ghi chú thử",
            null, null,
            ["VIP"], ["Thân thiết"],
            [], [],
            DateTimeOffset.UtcNow, 0, 0m, null, null);

    private static EntityCommentDto Comment(string author, string content, int daysAgo = 0) =>
        new(Guid.NewGuid(), "Customer", Id.ToString(), Guid.NewGuid(), author, content, [], [],
            DateTimeOffset.UtcNow.AddDays(-daysAgo), false);

    [Fact]
    public async Task Ho_so_co_hoi_co_du_truong_quyet_dinh()
    {
        var text = await Sheet(lead: Lead()).BuildAsync("Lead", Id.ToString());

        Assert.NotNull(text);
        Assert.Contains("Trần Văn A", text, StringComparison.Ordinal);
        Assert.Contains("Facebook", text, StringComparison.Ordinal);
        Assert.Contains("Contacted", text, StringComparison.Ordinal);
        Assert.Contains("Có số điện thoại: có", text, StringComparison.Ordinal);
        Assert.Contains("Có email: không", text, StringComparison.Ordinal);
    }

    /// <summary>Số điện thoại KHÔNG được vào hồ sơ — model chỉ cần biết có liên hệ được hay không.</summary>
    [Fact]
    public async Task Ho_so_khong_kem_so_dien_thoai()
    {
        var text = await Sheet(lead: Lead()).BuildAsync("Lead", Id.ToString());

        Assert.DoesNotContain("0900000001", text!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ho_so_khach_hang_co_phan_khuc_va_ghi_chu()
    {
        var text = await Sheet(customer: Customer()).BuildAsync("Customer", Id.ToString());

        Assert.Contains("Lý Kim Quân", text!, StringComparison.Ordinal);
        Assert.Contains("Cần Thơ", text!, StringComparison.Ordinal);
        Assert.Contains("Đi Đà Nẵng", text!, StringComparison.Ordinal);
        Assert.Contains("VIP", text!, StringComparison.Ordinal);
        Assert.Contains("Ghi chú thử", text!, StringComparison.Ordinal);
        Assert.Contains("1.500.000", text!, StringComparison.Ordinal);   // tiền kiểu Việt
    }

    /// <summary>
    /// Luồng trao đổi là phần có giá trị nhất trong hồ sơ — bản ghi chỉ có vài trường cố định, diễn
    /// biến thật nằm trong lời nhân viên ghi lại. Thiếu nó thì mọi tính năng AI đều nghèo đi.
    /// </summary>
    [Fact]
    public async Task Ho_so_kem_luong_trao_doi()
    {
        var text = await Sheet(customer: Customer(), comments:
            [
                Comment("Nguyễn Sale", "Khách hỏi tour Đà Nẵng tháng 9"),
                Comment("Trần Điều Hành", "Đã báo giá, chờ khách phản hồi"),
            ])
            .BuildAsync("Customer", Id.ToString());

        Assert.Contains("Khách hỏi tour Đà Nẵng tháng 9", text!, StringComparison.Ordinal);
        Assert.Contains("Nguyễn Sale", text!, StringComparison.Ordinal);
        Assert.Contains("Đã báo giá", text!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Chưa có trao đổi nào thì phải NÓI RÕ là thiếu dữ liệu. Im lặng bỏ qua thì model tự lấp bằng
    /// suy đoán và cho điểm cao cho một hồ sơ trống.
    /// </summary>
    [Fact]
    public async Task Chua_co_trao_doi_thi_noi_ro_la_thieu_du_lieu()
    {
        var text = await Sheet(customer: Customer()).BuildAsync("Customer", Id.ToString());

        Assert.Contains("chưa có trao đổi nào", text!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Xuống dòng trong bình luận bị gộp lại — mỗi dòng hồ sơ phải là một bình luận.</summary>
    [Fact]
    public async Task Binh_luan_nhieu_dong_khong_lam_vo_cau_truc_ho_so()
    {
        var text = await Sheet(customer: Customer(), comments: [Comment("A", "dòng một\ndòng hai")])
            .BuildAsync("Customer", Id.ToString());

        Assert.Contains("dòng một dòng hai", text!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Loai_ban_ghi_khong_ho_tro_thi_tra_null()
    {
        Assert.Null(await Sheet().BuildAsync("Order", Id.ToString()));
        Assert.Null(await Sheet().BuildAsync("Invoice", Id.ToString()));
    }

    [Fact]
    public async Task Id_khong_phai_guid_thi_tra_null_chu_khong_nem()
    {
        Assert.Null(await Sheet(lead: Lead()).BuildAsync("Lead", "khong-phai-guid"));
    }

    [Fact]
    public async Task Lay_duoc_ten_ban_ghi()
    {
        Assert.Equal("Trần Văn A", await Sheet(lead: Lead()).TitleAsync("Lead", Id.ToString()));
        Assert.Equal("Lý Kim Quân", await Sheet(customer: Customer()).TitleAsync("Customer", Id.ToString()));
        Assert.Null(await Sheet().TitleAsync("Order", Id.ToString()));
    }

    /// <summary>Biên số dòng trao đổi phải được truyền xuống, không phải lấy hết rồi cắt ở bộ nhớ.</summary>
    [Fact]
    public async Task Xin_dung_so_dong_trao_doi_toi_da()
    {
        var comments = new FakeCommentService([]);
        var sheet = new AiRecordSheet(new FakeLeadService(Lead()), new FakeCustomerService(null), comments);

        await sheet.BuildAsync("Lead", Id.ToString());

        Assert.Equal(AiRecordSheet.MaxComments, comments.LastTake);
    }
}
