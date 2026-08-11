using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Web;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>
/// Canh trần các danh mục nạp THẲNG vào ô chọn.
///
/// Vượt trần là kiểu hỏng tệ nhất: giao diện vẫn hiện bình thường, chỉ thiếu lựa chọn. Người dùng
/// thấy "không có nhà cung cấp đó" rồi kết luận sai là dữ liệu chưa nhập — không ai nghĩ danh sách
/// bị cắt, và không có lỗi nào ở đâu để lần ra.
///
/// Bài này biến nó thành BÀI ĐỎ trước khi tới tay người dùng. Chạm 80% trần là báo, không đợi 100%:
/// tới 100% thì đã có người không chọn được bản ghi rồi.
///
/// Chạm ngưỡng thì việc cần làm KHÔNG phải nâng trần, mà chuyển ô đó sang select2 gọi server —
/// khuôn đã có sẵn ở `?handler=CustomerSearch` của màn chăm sóc và báo giá.
/// </summary>
public class TranDanhMucTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TranDanhMucTests(AuthTestFactory factory) => _factory = factory;

    public static TheoryData<string, int, string> DanhMuc => new()
    {
        // tên bảng nghiệp vụ · trần đang đặt · nơi dùng
        { "nhà cung cấp", TranDanhMuc.NhaCungCap, "vé máy bay, quỹ vé, quỹ phòng, đặt dịch vụ, điều hành" },
        { "chuyến khởi hành", TranDanhMuc.Chuyen, "phân HDV, điều xe, báo giá" },
        { "đại lý", TranDanhMuc.DaiLy, "đặt chỗ và báo giá đại lý" },
        { "tour mẫu", TranDanhMuc.TourMau, "màn chuyến đi" },
    };

    [Theory]
    [MemberData(nameof(DanhMuc))]
    public async Task Danh_muc_chua_cham_tran(string ten, int tran, string noiDung)
    {
        _ = await _factory.SeedTenantUserAsync("tran-" + ten.GetHashCode(StringComparison.Ordinal).ToString("x", System.Globalization.CultureInfo.InvariantCulture));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var soDong = ten switch
        {
            "nhà cung cấp" => db.Providers.IgnoreQueryFilters().Count(x => !x.IsDeleted),
            "chuyến khởi hành" => db.TourDepartures.IgnoreQueryFilters().Count(x => !x.IsDeleted),
            "đại lý" => db.Agents.IgnoreQueryFilters().Count(x => !x.IsDeleted),
            "tour mẫu" => db.Tours.IgnoreQueryFilters().Count(x => !x.IsDeleted),
            _ => throw new InvalidOperationException("Danh mục chưa khai cách đếm: " + ten),
        };

        var nguong = (int)(tran * TranDanhMuc.NguongCanhBao);

        Assert.True(soDong <= nguong,
            $"Danh mục \"{ten}\" đang có {soDong} dòng, chạm {soDong * 100 / tran}% trần {tran} " +
            $"(dùng ở: {noiDung}).{Environment.NewLine}" +
            "Đừng nâng trần — nâng chỉ đẩy lùi ngày vỡ. Chuyển ô chọn đó sang select2 gọi server, " +
            "theo khuôn ?handler=CustomerSearch của màn chăm sóc/báo giá.");
    }

    /// <summary>
    /// Trần phải là hằng số có tên, không phải số viết thẳng tại lời gọi. Số viết thẳng thì không ai
    /// đối chiếu được với dữ liệu thật, và bài kiểm thử ở trên không biết phải canh cái gì.
    /// </summary>
    [Fact]
    public void Khong_con_tran_viet_thang_trong_trang()
    {
        var goc = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "TourKit.Api", "Pages");
        if (!Directory.Exists(goc))
        {
            return;   // chạy ngoài cây mã nguồn thì bỏ qua
        }

        var mau = new System.Text.RegularExpressions.Regex(
            @"_[a-zA-Z]+\.List[A-Za-z]*Async\(1,\s*\d{2,}\)", System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(5));

        var pham = Directory.EnumerateFiles(goc, "*.cshtml.cs", SearchOption.AllDirectories)
            .Select(f => (Tep: Path.GetFileName(Path.GetDirectoryName(f)!) + "/" + Path.GetFileName(f), Noi: File.ReadAllText(f)))
            .Where(x => mau.IsMatch(x.Noi))
            .Select(x => x.Tep)
            .ToList();

        Assert.True(pham.Count == 0,
            "Còn trần viết thẳng tại lời gọi ở: " + string.Join(", ", pham) +
            ". Dùng hằng số trong TranDanhMuc để TranDanhMucTests canh được.");
    }
}
