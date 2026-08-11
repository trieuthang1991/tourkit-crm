using TourKit.Application.Providers.Import;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Đọc bảng giá dịch vụ từ tệp theo mẫu.
///
/// Hai luật do chủ dự án chốt và được kiểm thẳng ở đây: dòng sai KHÔNG chặn cả lô, và bước này chỉ
/// đọc/kiểm chứ không ghi gì (việc ghi là lời gọi riêng sau khi người dùng duyệt).
/// </summary>
public class NhapDichVuTests
{
    private static readonly NhapDichVuService Svc = new();

    private static BangNhap Bang(string[] tieuDe, params string[][] dong) =>
        new(tieuDe, dong.Select(d => (IReadOnlyList<string>)d).ToList());

    private static readonly string[] CotKhachSan =
    [
        MauNhapDichVu.TenGoiGia, MauNhapDichVu.SoKhach, MauNhapDichVu.GiaHopDong, MauNhapDichVu.GiaCongBo,
        MauNhapDichVu.TienTe, MauNhapDichVu.GhiChu,
        MauNhapDichVu.GiaiDoanTu, MauNhapDichVu.GiaiDoanDen, MauNhapDichVu.LoaiNgay,
        MauNhapDichVu.ChiPhiNetNgay, MauNhapDichVu.GiaBanNgay,
    ];

    [Fact]
    public void Doc_duoc_dong_day_du_cua_khach_san()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng Deluxe", "2", "1200000", "1500000", "VND", "ghi chú",
             "2026-06-01", "2026-08-31", "Ngày lễ", "1100000", "1400000"]), ProviderType.Hotel);

        Assert.Empty(kq.Hong);
        var d = Assert.Single(kq.Nhan);
        Assert.Equal("Phòng Deluxe", d.Line!.PriceName);
        Assert.Equal(1_200_000m, d.Line.ContractPrice);
        Assert.Equal(2, d.Line.AmountOfPeople);
        Assert.Equal(new DateOnly(2026, 6, 1), d.Line.Profile!.PeriodFrom);
        Assert.Equal("Ngày lễ", d.Line.Profile.DayType);
        Assert.Equal(1_100_000m, d.Line.Profile.NetCostPerDay);
    }

    /// <summary>
    /// Luật chốt: dòng sai không kéo cả lô xuống. Dòng đúng vẫn nhận, dòng sai trả về kèm SỐ DÒNG
    /// trong tệp để người dùng mở ra sửa đúng chỗ.
    /// </summary>
    [Fact]
    public void Dong_sai_khong_chan_dong_dung()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1000000", "", "", "", "", "", "", "", ""],
            ["", "2", "1000000", "", "", "", "", "", "", "", ""],            // thiếu tên gói giá
            ["Phòng C", "2", "khong-phai-so", "", "", "", "", "", "", "", ""],
            ["Phòng D", "1", "500000", "", "", "", "", "", "", "", ""]), ProviderType.Hotel);

        Assert.Equal(2, kq.Nhan.Count);
        Assert.Equal(2, kq.Hong.Count);
        Assert.Equal([3, 4], kq.Hong.Select(x => x.SoDong));   // dòng 1 là tiêu đề
        Assert.Contains("Tên gói giá", kq.Hong[0].Loi[0], StringComparison.Ordinal);
        Assert.Contains("không phải số", kq.Hong[1].Loi[0], StringComparison.Ordinal);
    }

    /// <summary>Người dùng chép từ bảng giá NCC thì số luôn có dấu chấm phân cách nghìn.</summary>
    [Fact]
    public void Doc_duoc_so_co_dau_phan_cach_nghin()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1.200.000", "1.500.000", "", "", "", "", "", "", ""]), ProviderType.Hotel);

        Assert.Empty(kq.Hong);
        Assert.Equal(1_200_000m, kq.Nhan[0].Line!.ContractPrice);
    }

    /// <summary>Tiêu đề đi qua tay người: viết hoa, thiếu dấu, thừa khoảng trắng đều phải khớp.</summary>
    [Theory]
    [InlineData("TÊN GÓI GIÁ", "GIÁ HỢP ĐỒNG")]
    [InlineData("Ten goi gia", "Gia hop dong")]
    [InlineData(" Tên gói giá ", " Giá hợp đồng ")]
    public void Khop_tieu_de_bo_qua_hoa_thuong_va_dau(string tenCot, string giaCot)
    {
        var kq = Svc.XemTruoc(Bang([tenCot, giaCot], ["Phòng A", "900000"]), ProviderType.Restaurant);

        Assert.Empty(kq.Hong);
        Assert.Equal("Phòng A", kq.Nhan[0].Line!.PriceName);
    }

    /// <summary>Thiếu cột bắt buộc là hỏng cả TỆP — báo một lần, đừng lặp cùng một câu ở mọi dòng.</summary>
    [Fact]
    public void Thieu_cot_bat_buoc_thi_bao_mot_lan_o_dong_tieu_de()
    {
        var kq = Svc.XemTruoc(Bang([MauNhapDichVu.TenGoiGia], ["Phòng A"], ["Phòng B"]), ProviderType.Restaurant);

        Assert.Empty(kq.Nhan);
        var h = Assert.Single(kq.Hong);
        Assert.Equal(1, h.SoDong);
        Assert.Contains(MauNhapDichVu.GiaHopDong, h.Loi[0], StringComparison.Ordinal);
    }

    /// <summary>Excel hay để lại hàng rỗng ở cuối vùng dữ liệu — báo chúng là lỗi chỉ làm người dùng đi tìm lỗi ma.</summary>
    [Fact]
    public void Hang_trong_hoan_toan_thi_bo_qua_im_lang()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1000000", "", "", "", "", "", "", "", ""],
            ["", "", "", "", "", "", "", "", "", "", ""],
            ["", "", "", "", "", "", "", "", "", "", ""]), ProviderType.Hotel);

        Assert.Single(kq.Nhan);
        Assert.Empty(kq.Hong);
    }

    [Fact]
    public void Giai_doan_nguoc_thi_bao_loi()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1000000", "", "", "", "2026-08-31", "2026-06-01", "", "", ""]), ProviderType.Hotel);

        Assert.Single(kq.Hong);
        Assert.Contains("sớm hơn", kq.Hong[0].Loi[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2026-06-01")]
    [InlineData("01/06/2026")]
    [InlineData("1/6/2026")]
    public void Nhan_ca_hai_cach_go_ngay(string ngay)
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1000000", "", "", "", ngay, "", "", "", ""]), ProviderType.Hotel);

        Assert.Empty(kq.Hong);
        Assert.Equal(new DateOnly(2026, 6, 1), kq.Nhan[0].Line!.Profile!.PeriodFrom);
    }

    [Fact]
    public void Loai_ngay_ngoai_danh_sach_thi_bao_loi_kem_gia_tri_cho_phep()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "1000000", "", "", "", "", "", "Ngày gì đó", "", ""]), ProviderType.Hotel);

        Assert.Single(kq.Hong);
        Assert.Contains("Ngày thường", kq.Hong[0].Loi[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Cot_rieng_theo_loai_NCC_khac_nhau()
    {
        Assert.Contains(MauNhapDichVu.LoaiNgay, MauNhapDichVu.Cot(ProviderType.Hotel).Select(c => c.TieuDe));
        Assert.Contains(MauNhapDichVu.HanhTrinh, MauNhapDichVu.Cot(ProviderType.Airline).Select(c => c.TieuDe));
        Assert.DoesNotContain(MauNhapDichVu.LoaiNgay, MauNhapDichVu.Cot(ProviderType.Airline).Select(c => c.TieuDe));
        Assert.DoesNotContain(MauNhapDichVu.HanhTrinh, MauNhapDichVu.Cot(ProviderType.Restaurant).Select(c => c.TieuDe));
    }

    /// <summary>Tệp mẫu phải tự đọc lại được: mẫu không khớp bộ đọc là cái bẫy tinh vi nhất.</summary>
    [Fact]
    public void Tep_mau_tai_ve_phai_doc_lai_duoc_khong_loi()
    {
        foreach (var loai in new[] { ProviderType.Hotel, ProviderType.Airline, ProviderType.Restaurant })
        {
            var csv = MauNhapDichVu.MauCsv(loai);
            var hang = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Split(',').Select(o => o.Trim('"')).ToArray()).ToList();

            var kq = Svc.XemTruoc(new BangNhap(hang[0], [hang[1]]), loai);

            Assert.Empty(kq.Hong);
            Assert.Single(kq.Nhan);
        }
    }

    /// <summary>
    /// Bảng giá khách sạn ngoài đời ghi "Giá NET/đêm" và "Giá bán/đêm", không có cột nào tên
    /// "Giá hợp đồng". Không nhận hai cột đó làm giá thì một tài liệu đọc HOÀN TOÀN ĐÚNG vẫn cho ra
    /// 0 dòng dùng được — phát hiện khi đo thật với model trên file báo giá khách sạn.
    /// </summary>
    [Fact]
    public void Khach_san_chi_co_gia_theo_ngay_van_nhan_duoc()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Deluxe City View", "2", "", "", "", "", "2026-06-01", "2026-08-31", "", "1.200.000", "1.500.000"]),
            ProviderType.Hotel);

        Assert.Empty(kq.Hong);
        var d = Assert.Single(kq.Nhan);
        Assert.Equal(1_200_000m, d.Line!.ContractPrice);
        Assert.Equal(1_500_000m, d.Line.PublicPrice);

        // Vẫn giữ nguyên ở hồ sơ dòng: đây là giá THEO NGÀY, không phải chỉ là giá gói.
        Assert.Equal(1_200_000m, d.Line.Profile!.NetCostPerDay);
    }

    /// <summary>Có cả hai thì cột chính thắng — giá theo ngày chỉ là đường lùi, không phải đè lên.</summary>
    [Fact]
    public void Co_ca_hai_thi_gia_hop_dong_thang()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "800000", "950000", "", "", "", "", "", "1.200.000", "1.500.000"]),
            ProviderType.Hotel);

        Assert.Empty(kq.Hong);
        Assert.Equal(800_000m, kq.Nhan[0].Line!.ContractPrice);
        Assert.Equal(950_000m, kq.Nhan[0].Line!.PublicPrice);
    }

    /// <summary>Không có giá nào thì vẫn phải báo thiếu — nới cho khách sạn không được nới thành bỏ kiểm.</summary>
    [Fact]
    public void Khong_co_gia_nao_thi_van_bao_thieu()
    {
        var kq = Svc.XemTruoc(Bang(CotKhachSan,
            ["Phòng A", "2", "", "", "", "", "", "", "", "", ""]), ProviderType.Hotel);

        Assert.Single(kq.Hong);
        Assert.Contains(MauNhapDichVu.GiaHopDong, kq.Hong[0].Loi[0], StringComparison.Ordinal);
    }
}
