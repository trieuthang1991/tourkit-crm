using TourKit.Api.Authz;
using TourKit.Api.Comments;
using TourKit.Api.Routing;

namespace TourKit.Tests.Comments;

public class CommentableEntitiesTests
{
    /// <summary>
    /// Mỗi loại bản ghi kèm TRANG RAZOR sở hữu nó. Có cột thứ hai này thì bài kiểm tra link mới có
    /// nghĩa: chỉ kiểm "route có tồn tại không" là không đủ — lỗi thật vừa gặp là link trỏ tới một
    /// route CÓ THẬT nhưng của màn KHÁC.
    /// </summary>
    private static readonly (string Entity, string Page)[] Known =
    [
        ("Lead", "/Leads/Index"),
        ("SalesOpportunity", "/SalesOpportunities/Index"),
        ("Customer", "/Customers/Details"),
        ("Order", "/Orders/Detail"),
    ];

    [Fact]
    public void Moi_link_deu_tro_ve_dung_ban_ghi_chu_khong_phai_man_danh_sach()
    {
        foreach (var (name, _) in Known)
        {
            var entry = CommentableEntities.Find(name);
            Assert.NotNull(entry);

            // Không có {id} nghĩa là người nhận thông báo rơi vào màn danh sách và phải tự đi tìm
            // lại bản ghi giữa hàng nghìn dòng — thông báo mất gần hết tác dụng.
            Assert.Contains("{id}", entry!.LinkTemplate);
        }
    }

    /// <summary>
    /// Link phải trỏ đúng TRANG SỞ HỮU bản ghi, không chỉ trỏ tới một route có thật.
    ///
    /// Bảng CommentableEntities viết tay và không ai kiểm, nên đổi đường dẫn của một màn là link ở
    /// đây thành sai — vẫn mở ra một trang bình thường, chỉ là trang KHÁC, và không ai nối được lỗi
    /// ngược về lần đổi route. Đã xảy ra thật: tách /co-hoi thành hai màn mà quên sửa ở đây, khiến
    /// thông báo của khách tiềm năng dẫn sang màn cơ hội. Bản kiểm cũ chỉ hỏi "route có tồn tại
    /// không" nên vẫn xanh suốt.
    /// </summary>
    [Fact]
    public void Link_phai_tro_dung_trang_so_huu_ban_ghi()
    {
        foreach (var (name, page) in Known)
        {
            var entry = CommentableEntities.Find(name)!;

            // Bỏ query ("?mo={id}") và tham số cuối ("/{id}") để còn lại phần route tĩnh.
            var duong = entry.LinkTemplate.Split('?')[0].TrimStart('/');
            var tinh = duong.EndsWith("/{id}", StringComparison.Ordinal)
                ? duong[..^"/{id}".Length]
                : duong;

            var routeCuaTrang = RouteMap.Pages
                .Where(p => string.Equals(p.Page, page, StringComparison.Ordinal))
                .Select(p => p.Route.Split('/')[0])
                .ToHashSet(StringComparer.Ordinal);

            Assert.True(routeCuaTrang.Count > 0, $"{page} không có trong RouteMap.");
            Assert.True(routeCuaTrang.Contains(tinh.Split('/')[0]),
                $"{name} trỏ tới '/{tinh}' nhưng trang sở hữu nó là {page} " +
                $"(route: {string.Join(", ", routeCuaTrang)}).");
        }
    }

    [Fact]
    public void Link_thay_id_that_vao_dung_cho()
    {
        var entry = CommentableEntities.Find("Order")!;

        var url = CommentableEntities.LinkFor(entry, "3bc55e16-09bb-4929-923e-a3cd6eee8706");

        Assert.Equal("/don-hang/3bc55e16-09bb-4929-923e-a3cd6eee8706", url);
        Assert.DoesNotContain("{id}", url);
    }

    [Fact]
    public void Moi_ma_quyen_deu_ton_tai_trong_catalog()
    {
        var known = Permissions.All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, _) in Known)
        {
            var entry = CommentableEntities.Find(name)!;
            Assert.True(known.Contains(entry.ViewPermission),
                $"{name} khai mã quyền '{entry.ViewPermission}' không có trong Permissions.All");
        }
    }

    /// <summary>
    /// Cơ hội và khách tiềm năng là HAI loại khác nhau, không được dùng chung mã quyền.
    ///
    /// Gộp lại thì ai xem được khách tiềm năng cũng đọc được bình luận của cơ hội — mà bình luận
    /// cơ hội có giá, có thoả thuận, thường nhạy cảm hơn chính bản ghi.
    /// </summary>
    [Fact]
    public void Co_hoi_va_khach_tiem_nang_khong_dung_chung_quyen()
    {
        var lead = CommentableEntities.Find("Lead")!;
        var coHoi = CommentableEntities.Find("SalesOpportunity")!;

        Assert.NotEqual(lead.ViewPermission, coHoi.ViewPermission);
        Assert.NotEqual(lead.LinkTemplate, coHoi.LinkTemplate);
    }

    [Theory]
    [InlineData("User")]          // có thật trong hệ thống nhưng KHÔNG được bình luận
    [InlineData("SecretTable")]   // bịa ra
    [InlineData("lead")]          // sai hoa thường — phải khớp đúng tên type
    [InlineData("")]
    [InlineData(null)]
    public void Loai_ngoai_danh_sach_trang_bi_tu_choi(string? name)
    {
        Assert.Null(CommentableEntities.Find(name));
    }
}
