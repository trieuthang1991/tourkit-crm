namespace TourKit.Application.Finance.Dtos;

/// <summary>DTO tạo phiếu thu — tiền khách nộp cho một đơn.</summary>
public sealed record CreateReceiptDto(decimal Amount, string PaymentMethod, string? Partner, string? Note);

/// <summary>DTO phiếu thu trả ra cho client (không lộ entity).</summary>
public sealed record ReceiptDto(
    Guid Id, string Code, Guid OrderId, decimal Amount, string PaymentMethod,
    DateTimeOffset IssuedAt, string? Partner, string? Note, int Status, bool IsRecognized);

/// <summary>Công nợ phải thu của một đơn: tổng phải thu − đã thu (chỉ phiếu ĐÃ DUYỆT).</summary>
public sealed record OrderBalanceDto(Guid OrderId, decimal Total, decimal Paid, decimal Outstanding);

/// <summary>Dòng phiếu thu cho danh sách TỔNG (toàn tenant) — kèm mã đơn + tên khách để hiển thị.</summary>
public sealed record ReceiptListItemDto(
    Guid Id, string Code, Guid OrderId, string? OrderCode, string? CustomerName,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt, string? Partner, int Status, bool IsRecognized);
