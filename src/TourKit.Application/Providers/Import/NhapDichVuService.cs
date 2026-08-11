using System.Globalization;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Enums;
using TourKit.Shared.Text;

namespace TourKit.Application.Providers.Import;

/// <summary>
/// Bảng đã bóc từ tệp thành ô chữ: <c>TieuDe</c> là hàng tiêu đề, <c>Dong</c> là các hàng dữ liệu
/// chưa diễn giải. Tầng đọc tệp (CSV/XLSX) nằm ở ngoài — tầng này không biết định dạng nào cả.
/// </summary>
public sealed record BangNhap(IReadOnlyList<string> TieuDe, IReadOnlyList<IReadOnlyList<string>> Dong);

/// <summary>
/// Một dòng đọc được: hoặc hợp lệ (<c>Line</c> có giá trị), hoặc hỏng (<c>Loi</c> không rỗng).
///
/// <c>SoDong</c> là số dòng trong TỆP, tính cả hàng tiêu đề — người dùng mở tệp ra sửa đúng chỗ.
/// </summary>
public sealed record DongNhap(int SoDong, ProviderServiceLineDto? Line, IReadOnlyList<string> Loi, string? TenGoiGia);

/// <summary>Kết quả xem trước: <c>Nhan</c> là dòng nhận được, <c>Hong</c> là dòng hỏng kèm lý do.</summary>
public sealed record KetQuaXemTruoc(IReadOnlyList<DongNhap> Nhan, IReadOnlyList<DongNhap> Hong)
{
    public bool CoGiDeGhi => Nhan.Count > 0;
}

/// <summary>
/// Đọc bảng giá dịch vụ từ tệp theo mẫu, kiểm từng dòng rồi dựng bảng xem trước.
///
/// Hai luật do chủ dự án chốt, thể hiện thẳng ở đây:
///
/// 1. Dòng sai KHÔNG chặn cả lô — dòng đúng vẫn ghi được, dòng sai trả về kèm lý do và SỐ DÒNG trong
///    tệp để người dùng mở ra sửa đúng chỗ. Bảng giá NCC thường dài, bắt làm lại cả lô vì một ô là
///    đẩy việc sửa tay sang người dùng.
/// 2. Luôn có bước xem trước — lớp này chỉ ĐỌC và KIỂM, không ghi gì. Việc ghi là một lời gọi riêng
///    sau khi người dùng đã duyệt.
/// </summary>
public sealed class NhapDichVuService
{
    public KetQuaXemTruoc XemTruoc(BangNhap bang, ProviderType loai)
    {
        var cot = MauNhapDichVu.Cot(loai);
        var viTri = ViTriCot(bang.TieuDe);

        var thieu = cot.Where(c => c.BatBuoc && !viTri.ContainsKey(Khoa(c.TieuDe))).ToList();
        if (thieu.Count > 0)
        {
            // Thiếu cột bắt buộc là hỏng cả TỆP chứ không phải hỏng dòng: báo một lần ở dòng tiêu đề,
            // đừng lặp lại cùng một câu ở từng dòng rồi chôn mất thông tin thật.
            var ten = string.Join(", ", thieu.Select(c => c.TieuDe));
            return new KetQuaXemTruoc([], [new DongNhap(1, null, [$"Tệp thiếu cột bắt buộc: {ten}"], null)]);
        }

        var nhan = new List<DongNhap>();
        var hong = new List<DongNhap>();

        for (var i = 0; i < bang.Dong.Count; i++)
        {
            var soDong = i + 2;   // +1 vì đếm từ 1, +1 nữa vì hàng tiêu đề
            var dong = bang.Dong[i];

            string? O(string tieuDe) =>
                viTri.TryGetValue(Khoa(tieuDe), out var k) && k < dong.Count && !string.IsNullOrWhiteSpace(dong[k])
                    ? dong[k].Trim()
                    : null;

            // Hàng trống hoàn toàn thì bỏ qua im lặng: Excel hay để lại vài hàng rỗng ở cuối vùng dữ
            // liệu, báo chúng là "dòng hỏng" chỉ làm người dùng đi tìm một lỗi không có thật.
            if (cot.All(c => O(c.TieuDe) is null))
            {
                continue;
            }

            var loi = new List<string>();
            var ten = O(MauNhapDichVu.TenGoiGia);
            if (ten is null)
            {
                loi.Add($"Thiếu {MauNhapDichVu.TenGoiGia}");
            }

            var soKhach = SoNguyen(O(MauNhapDichVu.SoKhach), loi);
            var hoSo = HoSo(O, loai, loi);

            // Giá theo NGÀY của khách sạn chính là giá hợp đồng/công bố của gói, chỉ khác đơn vị tính.
            //
            // Bảng giá khách sạn ngoài đời không có cột nào tên "Giá hợp đồng" — nó ghi "Giá NET/đêm"
            // và "Giá bán/đêm". Không nhận hai cột đó làm giá thì một tài liệu đọc HOÀN TOÀN ĐÚNG vẫn
            // cho ra 0 dòng dùng được, toàn bộ báo "Thiếu Giá hợp đồng" — người dùng không hiểu phải
            // sửa gì vì tài liệu của họ có đủ giá.
            var giaHopDong = So(O(MauNhapDichVu.GiaHopDong), MauNhapDichVu.GiaHopDong,
                batBuoc: hoSo.NetCostPerDay is null, loi) ?? hoSo.NetCostPerDay;
            var giaCongBo = So(O(MauNhapDichVu.GiaCongBo), MauNhapDichVu.GiaCongBo, batBuoc: false, loi)
                ?? hoSo.SellPricePerDay;

            if (loi.Count > 0)
            {
                hong.Add(new DongNhap(soDong, null, loi, ten));
                continue;
            }

            nhan.Add(new DongNhap(soDong, new ProviderServiceLineDto(
                Id: null,
                ServiceItemId: null,
                PriceName: ten,
                ContractPrice: giaHopDong ?? 0m,
                PublicPrice: giaCongBo ?? 0m,
                CurrencyCode: O(MauNhapDichVu.TienTe) ?? "VND",
                AmountOfPeople: soKhach ?? 1,
                Note: O(MauNhapDichVu.GhiChu),
                Status: 1,
                Profile: hoSo), [], ten));
        }

        return new KetQuaXemTruoc(nhan, hong);
    }

    private static ProviderServiceLineProfile HoSo(Func<string, string?> O, ProviderType loai, List<string> loi)
    {
        if (ProviderServiceLineProfile.CoCotKhachSan(loai))
        {
            var tu = Ngay(O(MauNhapDichVu.GiaiDoanTu), MauNhapDichVu.GiaiDoanTu, loi);
            var den = Ngay(O(MauNhapDichVu.GiaiDoanDen), MauNhapDichVu.GiaiDoanDen, loi);

            // Giai đoạn ngược là lỗi NGHIỆP VỤ, không phải lỗi định dạng: cả hai ô đều đúng kiểu ngày
            // nên không chỗ nào khác bắt được, mà bảng giá "từ 31/8 đến 1/6" thì không áp được ngày nào.
            if (tu is not null && den is not null && den < tu)
            {
                loi.Add($"{MauNhapDichVu.GiaiDoanDen} sớm hơn {MauNhapDichVu.GiaiDoanTu}");
            }

            return new ProviderServiceLineProfile
            {
                PeriodFrom = tu,
                PeriodTo = den,
                DayType = MotTrong(O(MauNhapDichVu.LoaiNgay), MauNhapDichVu.LoaiNgay,
                    ProviderServiceLineProfile.DayTypes, loi),
                NetCostPerDay = So(O(MauNhapDichVu.ChiPhiNetNgay), MauNhapDichVu.ChiPhiNetNgay, false, loi),
                SellPricePerDay = So(O(MauNhapDichVu.GiaBanNgay), MauNhapDichVu.GiaBanNgay, false, loi),
            };
        }

        if (ProviderServiceLineProfile.CoCotVeMayBay(loai))
        {
            return new ProviderServiceLineProfile
            {
                TicketType = MotTrong(O(MauNhapDichVu.LoaiVe), MauNhapDichVu.LoaiVe,
                    ProviderServiceLineProfile.TicketTypes, loi),
                Route = O(MauNhapDichVu.HanhTrinh),
                DepartTime = O(MauNhapDichVu.GioDi),
                ReturnTime = O(MauNhapDichVu.GioVe),
                DepositDeadline = Ngay(O(MauNhapDichVu.HanCatCoc), MauNhapDichVu.HanCatCoc, loi),
                Baggage = O(MauNhapDichVu.HanhLy),
            };
        }

        return new ProviderServiceLineProfile();
    }

    /// <summary>
    /// Khớp tiêu đề bỏ qua hoa/thường, khoảng trắng thừa và DẤU tiếng Việt.
    ///
    /// Tệp đi qua tay người: "GIÁ HỢP ĐỒNG", "Gia hop dong", "Giá hợp đồng " đều là một cột. Bắt gõ
    /// chính xác từng dấu thì mẫu cố định trở thành cái bẫy chứ không phải sự chắc chắn.
    /// </summary>
    private static Dictionary<string, int> ViTriCot(IReadOnlyList<string> tieuDe)
    {
        var ra = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < tieuDe.Count; i++)
        {
            var k = Khoa(tieuDe[i]);
            if (k.Length > 0 && !ra.ContainsKey(k))
            {
                ra[k] = i;
            }
        }

        return ra;
    }

    private static string Khoa(string? s) =>
        (VietnameseText.NormalizeSearch(s ?? "") ?? "").Replace(" ", "", StringComparison.Ordinal);

    private static decimal? So(string? v, string tieuDe, bool batBuoc, List<string> loi)
    {
        if (v is null)
        {
            if (batBuoc)
            {
                loi.Add($"Thiếu {tieuDe}");
            }

            return null;
        }

        // Bỏ dấu phân cách nghìn kiểu Việt ("1.200.000") và khoảng trắng — người dùng chép thẳng từ
        // bảng giá của NCC, ở đó số luôn có dấu chấm.
        var sach = v.Replace(".", "", StringComparison.Ordinal)
                    .Replace(",", ".", StringComparison.Ordinal)
                    .Replace(" ", "", StringComparison.Ordinal);

        if (!decimal.TryParse(sach, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
        {
            loi.Add($"{tieuDe} không phải số: \"{v}\"");
            return null;
        }

        if (n < 0)
        {
            loi.Add($"{tieuDe} âm: {v}");
            return null;
        }

        return n;
    }

    private static int? SoNguyen(string? v, List<string> loi)
    {
        if (v is null)
        {
            return null;
        }

        if (!int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < 1)
        {
            loi.Add($"{MauNhapDichVu.SoKhach} phải là số nguyên từ 1: \"{v}\"");
            return null;
        }

        return n;
    }

    /// <summary>Nhận cả yyyy-MM-dd lẫn dd/MM/yyyy — hai cách người Việt gõ ngày trong Excel.</summary>
    private static DateOnly? Ngay(string? v, string tieuDe, List<string> loi)
    {
        if (v is null)
        {
            return null;
        }

        string[] dang = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"];
        if (DateOnly.TryParseExact(v, dang, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d;
        }

        loi.Add($"{tieuDe} không phải ngày hợp lệ: \"{v}\" (dùng yyyy-MM-dd hoặc dd/MM/yyyy)");
        return null;
    }

    /// <summary>Ô chỉ nhận một trong các giá trị cho sẵn; so không phân biệt hoa/thường và dấu.</summary>
    private static string? MotTrong(string? v, string tieuDe, IReadOnlyList<string> chapNhan, List<string> loi)
    {
        if (v is null)
        {
            return null;
        }

        var khop = chapNhan.FirstOrDefault(x => Khoa(x) == Khoa(v));
        if (khop is null)
        {
            loi.Add($"{tieuDe} phải là một trong: {string.Join(" / ", chapNhan)} — nhận được \"{v}\"");
        }

        return khop;
    }
}
