namespace TourKit.Application.Flights.Dtos;

/// <summary>DTO vé lẻ trả client — P/L derived (còn nợ thu/phải chi/lợi nhuận) + enrich tên NCC/Đơn.</summary>
public sealed record FlightTicketIndividualDto(
    Guid Id, string Code, string? TicketCode, string Pnr, string CustomerName,
    string? OrderRef, string? OrderCode, string? ProviderRef, string? ProviderName,
    int TripType, string? Route, DateTimeOffset? DepartDate, DateTimeOffset? ReturnDate,
    decimal SellAmount, decimal ReceivedAmount, decimal ReceivableRemaining,
    decimal TotalCost, decimal PaidAmount, decimal PayableRemaining, decimal Profit,
    DateTimeOffset? PaymentDueDate, int Status, string? AssigneeRef, string? Note);

/// <summary>
/// Bộ lọc vé lẻ. Tab (bám sub-tab hệ cũ): new · approved · rejected · pending-pay · due-soon ·
/// overdue · partial-pay · success · partial-receive (null = tất cả). Status: 0 tạo mới · 1 đã duyệt · 2 không duyệt.
/// </summary>
public sealed record FlightTicketIndividualListFilter(
    string? Q = null, string? ProviderRef = null, int? Status = null, string? Tab = null,
    DateTimeOffset? DepartFrom = null, DateTimeOffset? DepartTo = null);

/// <summary>Thẻ tổng + footer P/L (bám hệ cũ): đếm theo sub-tab + tổng thu/thực thu/tổng chi/thực chi/lợi nhuận/còn nợ/phải chi.</summary>
public sealed record FlightTicketIndividualStatsDto(
    int Total, int New, int Approved, int Rejected,
    int PendingPay, int DueSoon, int Overdue, int PartialPay, int Success, int PartialReceive,
    decimal TotalSell, decimal TotalReceived, decimal TotalReceivable,
    decimal TotalCost, decimal TotalPaid, decimal TotalPayable, decimal TotalProfit);

public sealed record CreateFlightTicketIndividualDto(
    string Code, string? TicketCode, string Pnr, string CustomerName,
    string? OrderRef, string? ProviderRef, int TripType, string? Route,
    DateTimeOffset? DepartDate, DateTimeOffset? ReturnDate,
    decimal SellAmount, decimal ReceivedAmount, decimal TotalCost, decimal PaidAmount,
    DateTimeOffset? PaymentDueDate, int Status, string? AssigneeRef, string? Note);

public sealed record UpdateFlightTicketIndividualDto(
    string Code, string? TicketCode, string Pnr, string CustomerName,
    string? OrderRef, string? ProviderRef, int TripType, string? Route,
    DateTimeOffset? DepartDate, DateTimeOffset? ReturnDate,
    decimal SellAmount, decimal ReceivedAmount, decimal TotalCost, decimal PaidAmount,
    DateTimeOffset? PaymentDueDate, int Status, string? AssigneeRef, string? Note);
