namespace TourKit.Shared.Domain;

/// <summary>
/// Danh sách tỉnh / thành phố trực thuộc trung ương, dùng cho mọi ô "Tỉnh thành".
///
/// Vì sao ghi trong MÃ chứ không thành bảng: hệ cũ cũng làm vậy (<c>UIHelper.GetProvince()</c>), và
/// đây là dữ liệu tham chiếu hành chính — nó chỉ đổi khi có nghị quyết sáp nhập, không phải thứ người
/// dùng thêm bớt hằng ngày như chi nhánh hay thị trường. Thành bảng thì phải kèm màn quản trị, quyền,
/// migration cho một danh sách vài năm mới đổi một lần.
///
/// Vì sao KHÔNG chép nguyên danh sách của hệ cũ: hệ cũ ghim 63 tỉnh theo đơn vị hành chính TRƯỚC
/// sáp nhập. Từ 01/07/2025 cả nước còn 34 đơn vị cấp tỉnh (28 tỉnh + 6 thành phố trực thuộc trung
/// ương). Bê nguyên danh sách cũ sang là đưa vào hệ thống mới một danh mục đã hết hiệu lực.
///
/// Giá trị lưu xuống CSDL là CHÍNH TÊN ở đây (cột <c>Province</c> kiểu chuỗi), không phải mã số —
/// giữ nguyên kiểu cột đang có nên không cần migration, và dữ liệu đọc được bằng mắt khi tra SQL.
/// </summary>
public static class TinhThanh
{
    /// <summary>6 thành phố trực thuộc trung ương, xếp trước cho dễ chọn vì dùng nhiều nhất.</summary>
    public static readonly IReadOnlyList<string> ThanhPhoTrungUong =
    [
        "Hà Nội", "Hồ Chí Minh", "Hải Phòng", "Đà Nẵng", "Huế", "Cần Thơ",
    ];

    /// <summary>28 tỉnh, xếp theo bảng chữ cái tiếng Việt.</summary>
    public static readonly IReadOnlyList<string> Tinh =
    [
        "An Giang", "Bắc Ninh", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Điện Biên", "Đồng Nai", "Đồng Tháp",
        "Gia Lai", "Hà Tĩnh", "Hưng Yên", "Khánh Hòa", "Lai Châu", "Lâm Đồng", "Lạng Sơn", "Lào Cai",
        "Nghệ An", "Ninh Bình", "Phú Thọ", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sơn La",
        "Tây Ninh", "Thái Nguyên", "Thanh Hóa", "Tuyên Quang", "Vĩnh Long",
    ];

    /// <summary>Toàn bộ 34 đơn vị cấp tỉnh: thành phố trực thuộc trung ương trước, rồi tới tỉnh.</summary>
    public static readonly IReadOnlyList<string> TatCa = [.. ThanhPhoTrungUong, .. Tinh];

    /// <summary>Tên này có nằm trong danh sách hiện hành không (so đúng chữ).</summary>
    public static bool Co(string? ten) =>
        ten is not null && TatCa.Contains(ten.Trim(), StringComparer.Ordinal);
}
