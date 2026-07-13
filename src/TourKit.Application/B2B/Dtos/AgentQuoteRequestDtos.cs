using TourKit.Shared.Enums;

namespace TourKit.Application.B2B.Dtos;

public sealed record AgentQuoteRequestDto(
    Guid Id, Guid AgentId, string ProductName, DateTimeOffset? TravelDate, DateTimeOffset? ReturnDate,
    int PaxCount, string? SpecialRequests, AgentQuoteStatus Status, decimal? QuotedAmount, string? QuotedNote,
    string? AgentName = null);

/// <summary>Bộ lọc yêu cầu báo giá đại lý: đại lý · trạng thái · từ khoá (sản phẩm). Status: 1 gửi,2 chào,3 xác nhận,4 từ chối.</summary>
public sealed record AgentQuoteRequestListFilter(string? Q = null, Guid? AgentId = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Báo giá đại lý: tổng + theo trạng thái + tổng giá đã chào.</summary>
public sealed record AgentQuoteStatsDto(int Total, int Requested, int Quoted, int Confirmed, int Rejected, decimal TotalQuoted);

/// <summary>Đại lý gửi yêu cầu báo giá.</summary>
public sealed record CreateAgentQuoteRequestDto(
    Guid AgentId, string ProductName, DateTimeOffset? TravelDate, DateTimeOffset? ReturnDate,
    int PaxCount, string? SpecialRequests);

/// <summary>Sales chào giá (điền số tiền + ghi chú) → chuyển Quoted.</summary>
public sealed record QuoteAgentRequestDto(decimal QuotedAmount, string? QuotedNote);
