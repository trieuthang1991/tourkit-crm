using TourKit.Api.Ai;
using TourKit.Application.Providers.Import;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Ghép dòng AI trả về thành bảng theo mẫu, rồi đưa qua CHÍNH lớp kiểm của đường Excel/CSV.
///
/// Đây là chỗ nối hai bước của IMPORT. Điểm mấu chốt: bảng dựng theo THỨ TỰ CỘT CỦA MẪU, không theo
/// key model trả về — model có thể bỏ sót cột, đảo thứ tự, hoặc bịa thêm key, mà lớp kiểm phía sau
/// lại đọc theo vị trí. Lệch một cột là toàn bộ dòng sai giá trị mà không có lỗi nào.
/// </summary>
public class AiBangGiaThanhBangTests
{
    private static List<string> Cot(ProviderType loai) =>
        MauNhapDichVu.Cot(loai).Select(c => c.TieuDe).ToList();

    private static Dictionary<string, string> Dong(params (string Key, string Value)[] o) =>
        o.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

    [Fact]
    public void Cot_model_bo_sot_thi_thanh_o_rong_chu_khong_lech_cot()
    {
        var cot = Cot(ProviderType.Hotel);

        // Model chỉ trả hai cột trong số cả chục cột của mẫu.
        var bang = AiBangGia.ThanhBang(
            [Dong(("Tên gói giá", "Phòng A"), ("Giá hợp đồng", "1000000"))], cot);

        Assert.Equal(cot, bang.TieuDe);
        Assert.Equal(cot.Count, bang.Dong[0].Count);

        var kq = new NhapDichVuService().XemTruoc(bang, ProviderType.Hotel);
        Assert.Empty(kq.Hong);
        Assert.Equal("Phòng A", kq.Nhan[0].Line!.PriceName);
        Assert.Equal(1_000_000m, kq.Nhan[0].Line!.ContractPrice);
    }

    [Fact]
    public void Model_dao_thu_tu_key_van_ra_dung_gia_tri()
    {
        var cot = Cot(ProviderType.Hotel);

        var bang = AiBangGia.ThanhBang(
            [Dong(("Loại ngày", "Ngày lễ"), ("Giá hợp đồng", "900000"), ("Tên gói giá", "Phòng B"))], cot);

        var kq = new NhapDichVuService().XemTruoc(bang, ProviderType.Hotel);
        Assert.Empty(kq.Hong);
        Assert.Equal("Phòng B", kq.Nhan[0].Line!.PriceName);
        Assert.Equal(900_000m, kq.Nhan[0].Line!.ContractPrice);
        Assert.Equal("Ngày lễ", kq.Nhan[0].Line!.Profile!.DayType);
    }

    [Fact]
    public void Key_la_model_tu_bia_thi_bi_bo_qua()
    {
        var cot = Cot(ProviderType.Restaurant);

        var bang = AiBangGia.ThanhBang(
            [Dong(("Tên gói giá", "Suất ăn"), ("Giá hợp đồng", "150000"), ("Cột không có trong mẫu", "rác"))], cot);

        Assert.Equal(cot.Count, bang.Dong[0].Count);
        Assert.DoesNotContain("rác", bang.Dong[0]);
    }

    /// <summary>
    /// Model đoán sai vẫn chỉ hỏng ĐÚNG dòng đó, có lý do rõ ràng — không đẩy được số rác vào bảng giá.
    /// Đây chính là lý do khâu kiểm không giao cho model.
    /// </summary>
    [Fact]
    public void Model_tra_gia_tri_bay_ba_thi_thanh_dong_hong_co_ly_do()
    {
        var cot = Cot(ProviderType.Hotel);

        var bang = AiBangGia.ThanhBang(
        [
            Dong(("Tên gói giá", "Phòng tốt"), ("Giá hợp đồng", "1000000")),
            Dong(("Tên gói giá", "Phòng lỗi"), ("Giá hợp đồng", "liên hệ")),
            Dong(("Tên gói giá", "Phòng ngày lạ"), ("Giá hợp đồng", "1000000"), ("Giai đoạn từ", "mùa hè")),
        ], cot);

        var kq = new NhapDichVuService().XemTruoc(bang, ProviderType.Hotel);

        Assert.Single(kq.Nhan);
        Assert.Equal(2, kq.Hong.Count);
        Assert.Contains("không phải số", kq.Hong[0].Loi[0], StringComparison.Ordinal);
        Assert.Contains("không phải ngày hợp lệ", kq.Hong[1].Loi[0], StringComparison.Ordinal);
    }

    /// <summary>Cột của mẫu khác nhau theo loại NCC, nên bảng AI dựng cũng phải khác theo.</summary>
    [Fact]
    public void Bang_dung_theo_dung_cot_cua_loai_NCC()
    {
        var bang = AiBangGia.ThanhBang(
            [Dong(("Tên gói giá", "VN123"), ("Giá hợp đồng", "2000000"), ("Hành trình", "SGN-HAN"))],
            Cot(ProviderType.Airline));

        var kq = new NhapDichVuService().XemTruoc(bang, ProviderType.Airline);
        Assert.Empty(kq.Hong);
        Assert.Equal("SGN-HAN", kq.Nhan[0].Line!.Profile!.Route);
    }
}
