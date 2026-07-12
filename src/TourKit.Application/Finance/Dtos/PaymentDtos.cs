namespace TourKit.Application.Finance.Dtos;

/// <summary>DTO tạo phiếu chi — tiền công ty chi trả cho NCC theo một đơn (đối xứng phiếu thu).</summary>
public sealed record CreatePaymentDto(
    Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod,
    string? Partner, string? ReceiverName, string? Note);

/// <summary>DTO phiếu chi trả ra cho client (không lộ entity).</summary>
public sealed record PaymentDto(
    Guid Id, string Code, Guid OrderId, Guid? ProviderId, Guid? OrderCostId,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt,
    string? Partner, string? ReceiverName, string? Note, int Status, bool IsRecognized);

/// <summary>Dòng phiếu chi cho danh sách TỔNG (toàn tenant) — kèm mã đơn + tên NCC để hiển thị.</summary>
public sealed record PaymentListItemDto(
    Guid Id, string Code, Guid OrderId, string? OrderCode, Guid? ProviderId, string? ProviderName,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt,
    string? Partner, string? ReceiverName, int Status, bool IsRecognized);

/// <summary>Bộ lọc danh sách phiếu chi (bám hệ cũ). Trạng thái: 0 chờ duyệt · 1 duyệt · 2 từ chối.</summary>
public sealed record PaymentListFilter(string? Q = null, int? Status = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

/// <summary>Thẻ thống kê đầu màn Phiếu chi: tổng phiếu + tổng tiền + đếm theo trạng thái.</summary>
public sealed record PaymentStatsDto(int Total, decimal TotalAmount, int Pending, int Approved, int Rejected);
