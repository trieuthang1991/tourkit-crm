namespace TourKit.Api.Pages.Shared;

/// <summary>
/// Chuẩn hoá NGÀY NGHIỆP VỤ (ngày khởi hành, ngày sinh, hạn thanh toán…) trước khi ghi xuống DB.
///
/// Vì sao cần: ô ngày trên giao diện gửi lên chuỗi "2026-08-15" không kèm múi giờ. ASP.NET bind
/// thành DateTimeOffset theo giờ MÁY CHỦ (VN = +07:00) → 2026-08-15T00:00+07:00. Cột timestamptz
/// của Postgres lưu theo UTC nên giá trị thành 2026-08-14T17:00Z, và màn hình đọc lại ra 14/08 —
/// LÙI ĐÚNG MỘT NGÀY so với cái người dùng gõ. Gọi ToUniversalTime() cũng ra y hệt, không cứu được.
///
/// Cách trị: bỏ phần giờ, neo thẳng mốc 0. Ngày nghiệp vụ không có múi giờ — 15/08 là 15/08.
/// KHÔNG dùng cho mốc thời gian thật (giờ nhắc việc, giờ tạo bản ghi): những chỗ đó phải giữ UTC.
/// </summary>
public static class TkDate
{
    public static DateTimeOffset Day(DateTimeOffset value) => new(value.DateTime.Date, TimeSpan.Zero);

    public static DateTimeOffset? Day(DateTimeOffset? value) => value is null ? null : Day(value.Value);
}
