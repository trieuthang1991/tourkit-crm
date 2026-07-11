namespace TourKit.Application.Booking.Dtos;

/// <summary>
/// Dữ liệu in hợp đồng tour (legacy contract_tour): gom đơn + khách + chuyến + điều khoản mẫu để render
/// hợp đồng dịch vụ du lịch (bản in cố định, không phải bộ dựng HĐ động).
/// </summary>
public sealed record OrderContractDto(
    string OrderCode,
    string CustomerName, string? CustomerPhone, string? CustomerAddress, string? CustomerIdCard, string? CustomerPassport,
    string TourTitle, DateTimeOffset? DepartureDate, DateTimeOffset? EndDate,
    int AdultCount, int ChildCount, int InfantCount,
    decimal TotalRevenue, string? Terms);
