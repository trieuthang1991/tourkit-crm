namespace TourKit.Api.Web;

/// <summary>
/// Trần số dòng khi nạp một danh mục THẲNG vào ô chọn.
///
/// Vì sao phải có chỗ này: mỗi màn trước đây tự đặt một con số (200, 500, 1000) ngay tại lời gọi.
/// Con số đó không nói gì về việc danh mục hiện có bao nhiêu dòng, nên khi dữ liệu vượt trần thì
/// giao diện vẫn hiện bình thường — chỉ thiếu lựa chọn. Người dùng thấy "không có nhà cung cấp đó"
/// và kết luận sai là dữ liệu chưa nhập, chứ không ai nghĩ tới chuyện danh sách bị cắt.
///
/// Đặt tên và gom một chỗ để <c>TranDanhMucTests</c> đối chiếu được với số dòng THẬT trong CSDL và
/// bắt lỗi TRƯỚC khi người dùng gặp. Chạm ngưỡng thì việc cần làm không phải nâng trần, mà chuyển ô
/// đó sang select2 gọi server như <c>?handler=CustomerSearch</c> đang làm với khách hàng.
/// </summary>
public static class TranDanhMuc
{
    /// <summary>Nhà cung cấp — dùng ở màn vé, quỹ vé, quỹ phòng, đặt dịch vụ, điều hành.</summary>
    public const int NhaCungCap = 1000;

    /// <summary>Chuyến khởi hành — màn phân HDV, điều xe, báo giá.</summary>
    public const int Chuyen = 1000;

    /// <summary>Đại lý — màn đặt chỗ và báo giá đại lý.</summary>
    public const int DaiLy = 500;

    /// <summary>Tour mẫu — màn chuyến đi.</summary>
    public const int TourMau = 1000;

    /// <summary>Xe — màn điều xe.</summary>
    public const int Xe = 1000;

    /// <summary>Đơn hàng — chỉ dùng cho ô chọn nhanh, không phải danh sách nghiệp vụ.</summary>
    public const int DonHang = 200;

    /// <summary>
    /// Tỉ lệ chạm trần coi là "sắp vỡ". Cảnh báo ở 80% chứ không đợi 100%: tới lúc đủ 100% thì đã có
    /// người dùng không chọn được bản ghi rồi.
    /// </summary>
    public const double NguongCanhBao = 0.8;
}
