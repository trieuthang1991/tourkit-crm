using TourKit.Application.Crm;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Luật chia số cho nhóm sale. Đây là chỗ quyết định việc của người ta, nên phủ cạn các nhánh —
/// chia lệch thì người bị thiệt không có cách nào tự phát hiện ra.
/// </summary>
public class ChiaSoSaleTests
{
    private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid C = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid[] Nhom = [A, B, C];

    [Fact]
    public void Xoay_vong_chia_deu_va_quay_lai_dung_thu_tu()
    {
        var nhan = Enumerable.Range(0, 7)
            .Select(i => ChiaSoSale.Chon(LeadAssignMode.XoayVong, Nhom, i))
            .ToList();

        Assert.Equal([A, B, C, A, B, C, A], nhan);
    }

    /// <summary>
    /// Chia đều tuyệt đối sau một vòng trọn: không ai nhận nhiều hơn người khác quá 1.
    /// </summary>
    [Fact]
    public void Xoay_vong_khong_ai_nhan_lech_qua_mot_so()
    {
        var dem = Enumerable.Range(0, 100)
            .Select(i => ChiaSoSale.Chon(LeadAssignMode.XoayVong, Nhom, i))
            .GroupBy(x => x)
            .Select(g => g.Count())
            .ToList();

        Assert.True(dem.Max() - dem.Min() <= 1, $"Lệch {dem.Max() - dem.Min()} số giữa người nhiều nhất và ít nhất.");
    }

    [Fact]
    public void Ngau_nhien_lay_dung_nguoi_theo_chi_so_duoc_tra_ve()
    {
        Assert.Equal(C, ChiaSoSale.Chon(LeadAssignMode.NgauNhien, Nhom, 0, _ => 2));
        Assert.Equal(A, ChiaSoSale.Chon(LeadAssignMode.NgauNhien, Nhom, 99, _ => 0));
    }

    /// <summary>
    /// Nhóm rỗng thì trả null chứ KHÔNG ném: lead vẫn phải vào được, chỉ là chưa ai phụ trách.
    /// Ném lỗi ở đây nghĩa là form thu lead trả lỗi cho khách vì một chuyện nội bộ của công ty.
    /// </summary>
    [Fact]
    public void Nhom_rong_thi_khong_chia_chu_khong_nem()
    {
        Assert.Null(ChiaSoSale.Chon(LeadAssignMode.XoayVong, [], 0));
        Assert.Null(ChiaSoSale.Chon(LeadAssignMode.NgauNhien, [], 0));
    }

    [Fact]
    public void Che_do_khong_tu_chia_thi_de_trong_nguoi_phu_trach()
    {
        Assert.Null(ChiaSoSale.Chon(LeadAssignMode.KhongTuChia, Nhom, 0));
    }

    /// <summary>Một người thì mọi số đều về người đó, không lỗi chia dư.</summary>
    [Fact]
    public void Nhom_mot_nguoi_thi_moi_so_ve_nguoi_do()
    {
        Assert.Equal(A, ChiaSoSale.Chon(LeadAssignMode.XoayVong, [A], 0));
        Assert.Equal(A, ChiaSoSale.Chon(LeadAssignMode.XoayVong, [A], 41));
    }

    /// <summary>
    /// Con đếm âm không được làm sập. Nó không xảy ra trong luồng bình thường, nhưng một phép
    /// chia dư âm sẽ ném IndexOutOfRange — và chỗ ném là giữa đường nhận lead của khách.
    /// </summary>
    [Fact]
    public void Con_dem_am_van_chon_duoc_nguoi()
    {
        Assert.Equal(B, ChiaSoSale.Chon(LeadAssignMode.XoayVong, Nhom, -1));
    }
}
