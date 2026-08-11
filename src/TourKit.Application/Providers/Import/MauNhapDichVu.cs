using TourKit.Shared.Enums;

namespace TourKit.Application.Providers.Import;

/// <summary>Một cột của mẫu nhập bảng giá dịch vụ.</summary>
/// <param name="TieuDe">Tiêu đề đúng như trong tệp mẫu.</param>
/// <param name="BatBuoc">Bỏ trống là dòng đó hỏng.</param>
public sealed record CotNhap(string TieuDe, bool BatBuoc = false);

/// <summary>
/// Mẫu cột của tệp nhập bảng giá — nguồn duy nhất cho cả tệp mẫu tải về lẫn bước đọc tệp lên.
///
/// Chủ dự án chốt: tệp Excel/CSV phải THEO MẪU do hệ thống đưa ra (khác đường PDF/DOCX, nơi AI mới
/// phải đoán). Nhờ vậy đường này tất định — cùng một tệp luôn cho cùng kết quả, không phụ thuộc model.
///
/// Cột riêng theo loại NCC bám <see cref="ProviderServiceLineProfile"/>: khách sạn có giai đoạn/loại
/// ngày, hàng không có hành trình/giờ bay/hạn cắt cọc/hành lý.
/// </summary>
public static class MauNhapDichVu
{
    public const string TenGoiGia = "Tên gói giá";
    public const string SoKhach = "Số khách";
    public const string GiaHopDong = "Giá hợp đồng";
    public const string GiaCongBo = "Giá công bố";
    public const string TienTe = "Tiền tệ";
    public const string GhiChu = "Ghi chú";

    public const string GiaiDoanTu = "Giai đoạn từ";
    public const string GiaiDoanDen = "Giai đoạn đến";
    public const string LoaiNgay = "Loại ngày";
    public const string ChiPhiNetNgay = "Chi phí NET/ngày";
    public const string GiaBanNgay = "Giá bán/ngày";

    public const string LoaiVe = "Loại vé";
    public const string HanhTrinh = "Hành trình";
    public const string GioDi = "Giờ đi";
    public const string GioVe = "Giờ về";
    public const string HanCatCoc = "Hạn cắt cọc";
    public const string HanhLy = "Hành lý";

    private static readonly CotNhap[] Chung =
    [
        new(TenGoiGia, BatBuoc: true),
        new(SoKhach),
        new(GiaHopDong, BatBuoc: true),
        new(GiaCongBo),
        new(TienTe),
        new(GhiChu),
    ];

    private static readonly CotNhap[] KhachSan =
    [
        new(GiaiDoanTu), new(GiaiDoanDen), new(LoaiNgay), new(ChiPhiNetNgay), new(GiaBanNgay),
    ];

    private static readonly CotNhap[] VeMayBay =
    [
        new(LoaiVe), new(HanhTrinh), new(GioDi), new(GioVe), new(HanCatCoc), new(HanhLy),
    ];

    /// <summary>Cột của mẫu ứng với loại NCC — chung trước, riêng sau.</summary>
    public static IReadOnlyList<CotNhap> Cot(ProviderType loai)
    {
        if (ProviderServiceLineProfile.CoCotKhachSan(loai))
        {
            return [.. Chung, .. KhachSan];
        }

        if (ProviderServiceLineProfile.CoCotVeMayBay(loai))
        {
            return [.. Chung, .. VeMayBay];
        }

        return Chung;
    }

    /// <summary>
    /// Nội dung tệp mẫu (CSV), gồm hàng tiêu đề và MỘT dòng ví dụ.
    ///
    /// Có dòng ví dụ vì mẫu trống không nói được định dạng ngày hay cách viết "Loại ngày" — người
    /// dùng sẽ đoán, đoán sai, rồi cả tệp hỏng ở bước xem trước mà không hiểu vì sao.
    /// </summary>
    public static string MauCsv(ProviderType loai)
    {
        var cot = Cot(loai);
        var tieuDe = string.Join(',', cot.Select(c => Boc(c.TieuDe)));
        var viDu = string.Join(',', cot.Select(c => Boc(ViDu(c.TieuDe))));
        return tieuDe + "\r\n" + viDu + "\r\n";
    }

    private static string Boc(string v) => "\"" + v.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string ViDu(string tieuDe) => tieuDe switch
    {
        TenGoiGia => "Phòng Deluxe 2 khách",
        SoKhach => "2",
        GiaHopDong => "1200000",
        GiaCongBo => "1500000",
        TienTe => "VND",
        GhiChu => "Áp dụng ngày thường",
        GiaiDoanTu => "2026-06-01",
        GiaiDoanDen => "2026-08-31",
        LoaiNgay => "Ngày thường",
        ChiPhiNetNgay => "1100000",
        GiaBanNgay => "1400000",
        LoaiVe => "Vé series",
        HanhTrinh => "SGN-HAN-SGN",
        GioDi => "08:30",
        GioVe => "17:45",
        HanCatCoc => "2026-05-20",
        HanhLy => "23kg",
        _ => "",
    };
}
