using TourKit.Shared.Domain;

namespace TourKit.UnitTests;

/// <summary>
/// Danh mục tỉnh / thành phố trực thuộc trung ương.
///
/// Trước đây hệ thống có HAI danh sách: <c>VietnamProvinces</c> ở tầng Api (63 tỉnh trước sáp nhập,
/// dùng cho màn khách hàng) và một bản mới cho màn nhà cung cấp. Hai danh mục cùng nghĩa trong một hệ
/// thống thì chắc chắn trôi lệch nhau, và lúc đó lọc theo địa bàn ở hai màn cho ra hai kết quả khác
/// nhau mà không ai hiểu vì sao. Nay gộp về đây.
/// </summary>
public class TinhThanhTests
{
    /// <summary>
    /// Từ 01/07/2025 cả nước còn 34 đơn vị cấp tỉnh: 28 tỉnh + 6 thành phố trực thuộc trung ương.
    /// Con số này là mốc kiểm: sửa danh sách mà lệch tổng thì hoặc thêm nhầm, hoặc sót.
    /// </summary>
    [Fact]
    public void Du_34_don_vi_cap_tinh()
    {
        Assert.Equal(6, TinhThanh.ThanhPhoTrungUong.Count);
        Assert.Equal(28, TinhThanh.Tinh.Count);
        Assert.Equal(34, TinhThanh.TatCa.Count);
    }

    /// <summary>Đơn vị đã sáp nhập không được còn trong danh mục — đó là cả lý do bỏ danh sách cũ.</summary>
    [Theory]
    [InlineData("Bà Rịa - Vũng Tàu")]
    [InlineData("Bình Dương")]
    [InlineData("Bắc Giang")]
    [InlineData("Hà Giang")]
    [InlineData("Bình Thuận")]
    public void Khong_con_don_vi_da_sap_nhap(string ten)
    {
        Assert.DoesNotContain(ten, TinhThanh.TatCa);
        Assert.False(TinhThanh.Co(ten));
    }

    [Theory]
    [InlineData("Hà Nội")]
    [InlineData("Hồ Chí Minh")]
    [InlineData("Đồng Nai")]
    [InlineData("Khánh Hòa")]
    public void Co_don_vi_hien_hanh(string ten)
    {
        Assert.True(TinhThanh.Co(ten));
    }

    /// <summary>Đồng Nai là TỈNH, không phải thành phố trực thuộc trung ương — nguồn tra cứu xếp sai chỗ này.</summary>
    [Fact]
    public void Dong_Nai_la_tinh_khong_phai_thanh_pho_trung_uong()
    {
        Assert.Contains("Đồng Nai", TinhThanh.Tinh);
        Assert.DoesNotContain("Đồng Nai", TinhThanh.ThanhPhoTrungUong);
    }

    [Fact]
    public void Khong_co_ten_trung_nhau()
    {
        Assert.Equal(TinhThanh.TatCa.Count, TinhThanh.TatCa.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Khoảng trắng thừa ở hai đầu vẫn phải nhận ra — giá trị đi qua form hay tệp nhập hay dính.</summary>
    [Fact]
    public void Bo_qua_khoang_trang_hai_dau()
    {
        Assert.True(TinhThanh.Co("  Hà Nội  "));
        Assert.False(TinhThanh.Co(null));
        Assert.False(TinhThanh.Co(""));
    }
}
