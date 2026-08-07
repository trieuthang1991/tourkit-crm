using TourKit.Api.Authz;
using TourKit.Api.Comments;

namespace TourKit.Tests.Comments;

public class CommentableEntitiesTests
{
    private static readonly string[] Known = ["Lead", "Customer", "Order"];

    [Fact]
    public void Moi_link_deu_tro_ve_dung_ban_ghi_chu_khong_phai_man_danh_sach()
    {
        foreach (var name in Known)
        {
            var entry = CommentableEntities.Find(name);
            Assert.NotNull(entry);

            // Không có {id} nghĩa là người nhận thông báo rơi vào màn danh sách và phải tự đi tìm
            // lại bản ghi giữa hàng nghìn dòng — thông báo mất gần hết tác dụng.
            Assert.Contains("{id}", entry!.LinkTemplate);
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

        foreach (var name in Known)
        {
            var entry = CommentableEntities.Find(name)!;
            Assert.True(known.Contains(entry.ViewPermission),
                $"{name} khai mã quyền '{entry.ViewPermission}' không có trong Permissions.All");
        }
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
