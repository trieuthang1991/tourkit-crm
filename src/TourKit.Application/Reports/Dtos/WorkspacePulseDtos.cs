namespace TourKit.Application.Reports.Dtos;

/// <summary>Một điểm trên biểu đồ doanh thu theo ngày (tiền ĐÃ ghi nhận, không phải tiền ghi sổ đơn).</summary>
public sealed record RevenuePointDto(DateTimeOffset Day, decimal Amount);

/// <summary>
/// Số liệu "nhịp" của màn Bàn làm việc: chuỗi doanh thu theo ngày + các mốc cộng dồn
/// (hôm nay/tuần/tháng/năm) + số liệu kỳ TRƯỚC để tính mức tăng giảm.
///
/// Gộp vào MỘT lần gọi thay vì mỗi thẻ một truy vấn: màn này là thứ mở đầu tiên mỗi sáng,
/// mở chậm là cả ngày thấy chậm.
/// </summary>
public sealed record WorkspacePulseDto(
    IReadOnlyList<RevenuePointDto> Series,
    decimal Today, decimal ThisWeek, decimal ThisMonth, decimal ThisYear,
    decimal Revenue7, decimal RevenuePrev7,
    int Orders7, int OrdersPrev7,
    int Customers7, int CustomersPrev7);
