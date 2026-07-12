namespace TourKit.Application.Sales.Dtos;

public sealed record QuoteLineDto(
    Guid Id, string Description, int Quantity, decimal UnitPrice, decimal Amount,
    int ServiceType, int Scope, Guid? ProviderServiceId, decimal UnitCost, decimal MarginPercent);

public sealed record QuoteDto(
    Guid Id, string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, decimal TotalAmount, QuoteLineDto[] Lines,
    int Adults, int Children, int Infants, decimal ChildPercent, decimal InfantPercent,
    decimal TotalCost, decimal TotalProfit,
    decimal AdultPrice, decimal ChildPrice, decimal InfantPrice,  // 3 giá hạng khách — derived, không lưu
    Guid? ConvertedOrderId);                                       // đơn đã sinh (null = chưa chuyển)

/// <summary>Dòng tóm tắt cho danh sách (không kèm chi tiết dòng).</summary>
public sealed record QuoteSummaryDto(
    Guid Id, string Code, string CustomerName, string Title, DateTimeOffset? ValidUntil, int Status, decimal TotalAmount,
    Guid? ConvertedOrderId, int Adults, int Children, int Infants, decimal TotalCost, decimal TotalProfit);

/// <summary>Bộ lọc danh sách báo giá (bám hệ cũ). Status: 0 nháp · 1 đã gửi · 2 chấp nhận · 3 từ chối. Converted: đã chuyển đơn.</summary>
public sealed record QuoteListFilter(
    string? Q = null, int? Status = null, DateTimeOffset? ValidFrom = null, DateTimeOffset? ValidTo = null,
    bool? Converted = null);

/// <summary>Thẻ thống kê đầu màn Báo giá: tổng + theo trạng thái + tổng giá trị + lợi nhuận dự kiến.</summary>
public sealed record QuoteStatsDto(
    int Total, int Draft, int Sent, int Accepted, int Rejected, decimal TotalAmount, decimal TotalProfit);

public sealed record CreateQuoteLineDto(
    string Description, int Quantity, decimal UnitPrice,
    int ServiceType = 0, int Scope = 0, Guid? ProviderServiceId = null,
    decimal UnitCost = 0, decimal MarginPercent = 0);

public sealed record CreateQuoteDto(
    string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, CreateQuoteLineDto[] Lines,
    int Adults = 0, int Children = 0, int Infants = 0,
    decimal ChildPercent = 75, decimal InfantPercent = 50);

/// <summary>
/// Chuyển báo giá chấp nhận → đơn. Hai chế độ:
/// - Ghép chuyến sẵn có: truyền <paramref name="TourDepartureId"/>.
/// - Tour lẻ FIT (legacy SingleTour): bỏ trống chuyến + truyền <paramref name="DepartureDate"/>
///   → hệ tự tạo CHUYẾN RIÊNG (không template, TotalSlots = đúng số khách báo giá — chuyến kín).
/// </summary>
public sealed record ConvertQuoteDto(Guid? TourDepartureId, DateTimeOffset? DepartureDate = null, DateTimeOffset? EndDate = null);

public sealed record ConvertQuoteResultDto(Guid OrderId, string OrderCode, int ServiceBookingCount);

public sealed record UpdateQuoteDto(
    string Code, Guid? CustomerId, string CustomerName, string Title,
    DateTimeOffset? ValidUntil, int Status, string? Note, CreateQuoteLineDto[] Lines,
    int Adults = 0, int Children = 0, int Infants = 0,
    decimal ChildPercent = 75, decimal InfantPercent = 50);
