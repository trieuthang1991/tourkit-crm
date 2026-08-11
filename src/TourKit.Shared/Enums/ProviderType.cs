namespace TourKit.Shared.Enums;

public enum ProviderType
{
    Hotel = 1,
    Vehicle = 2,
    Restaurant = 3,
    Guide = 4,
    Airline = 5,
    Other = 6,

    /// <summary>
    /// Voucher / dịch vụ trọn gói — hệ cũ có màn sửa riêng <c>EditVoucher.aspx</c>.
    ///
    /// Nối tiếp số 7 chứ KHÔNG lấy số 2 như <c>ServicesType.Vouchers</c> bên hệ cũ: số 2 ở đây đã là
    /// Vehicle và đang có dữ liệu. Đánh lại số là đổi nghĩa mọi dòng NCC đã lưu.
    /// </summary>
    Voucher = 7,
}
