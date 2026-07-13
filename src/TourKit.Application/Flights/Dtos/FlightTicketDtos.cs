namespace TourKit.Application.Flights.Dtos;

/// <summary>DTO vé đoàn trả client — kèm tên Thị trường/NCC/Đơn (enrich) + số còn lại (computed).</summary>
public sealed record FlightTicketDto(
    Guid Id, string Pnr, string? MarketRef, string? MarketName, string? ProviderRef, string? ProviderName,
    string? TourType, int Days, DateTimeOffset? DepartureDate,
    int Quantity, int UsedQuantity, int RemainingQuantity,
    string? OrderRef, string? OrderCode, string? OrderName,
    decimal TotalCost, decimal PaidAmount, decimal RemainingCost, decimal ReservedAmount,
    int Status, string? Note, IReadOnlyList<FlightSegment> Segments);

/// <summary>Bộ lọc vé đoàn: PNR · thị trường · NCC · loại hình · số ngày · ngày đi từ · đã/chưa gán tour.</summary>
public sealed record FlightTicketListFilter(
    string? Q = null, string? MarketRef = null, string? ProviderRef = null, string? TourType = null,
    int? Days = null, DateTimeOffset? DepartureFrom = null, bool? Assigned = null);

/// <summary>Thẻ tổng cuối lưới (bám footer hệ cũ): tổng vé · đã dùng · còn lại · tổng chi · đã TT · còn lại · bảo lưu.</summary>
public sealed record FlightTicketStatsDto(
    int Total, int Assigned, int Unassigned,
    int TotalQuantity, int TotalUsed, int TotalRemaining,
    decimal TotalCost, decimal TotalPaid, decimal TotalRemainingCost, decimal TotalReserved);

public sealed record CreateFlightTicketDto(
    string Pnr, string? MarketRef, string? ProviderRef, string? TourType, int Days, DateTimeOffset? DepartureDate,
    int Quantity, decimal TotalCost, decimal ReservedAmount, string? Note, IReadOnlyList<FlightSegment>? Segments);

public sealed record UpdateFlightTicketDto(
    string Pnr, string? MarketRef, string? ProviderRef, string? TourType, int Days, DateTimeOffset? DepartureDate,
    int Quantity, int UsedQuantity, string? OrderRef, decimal TotalCost, decimal PaidAmount, decimal ReservedAmount,
    int Status, string? Note, IReadOnlyList<FlightSegment>? Segments);

/// <summary>Gán vé đoàn vào 1 đơn/tour (hoặc null để gỡ gán).</summary>
public sealed record AssignFlightTicketDto(string? OrderRef);
