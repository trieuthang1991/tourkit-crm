namespace TourKit.Application.Finance.Dtos;

public sealed record TicketFundDto(
    Guid Id, Guid OrderId, Guid? ProviderId, Guid? ProviderServiceId, string TicketCode, int Status, bool IsClosed,
    string? OrderCode = null, string? ProviderName = null);

/// <summary>Bộ lọc quỹ vé ứng (bám hệ cũ): NCC · đơn · trạng thái · đã đóng · từ khoá (mã vé).</summary>
public sealed record TicketFundListFilter(
    string? Q = null, Guid? ProviderId = null, Guid? OrderId = null, int? Status = null, bool? IsClosed = null);

/// <summary>Thẻ thống kê đầu màn Quỹ vé ứng: tổng + đã đóng + chưa đóng.</summary>
public sealed record TicketFundStatsDto(int Total, int Closed, int Open);

public sealed record CreateTicketFundDto(
    Guid OrderId, Guid? ProviderId, Guid? ProviderServiceId, string? TicketCode, int Status, bool IsClosed);

public sealed record UpdateTicketFundDto(
    Guid? ProviderId, Guid? ProviderServiceId, string? TicketCode, int Status, bool IsClosed);
