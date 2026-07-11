using TourKit.Shared.Enums;

namespace TourKit.Application.B2B.Dtos;

public sealed record AgentQuoteRequestDto(
    Guid Id, Guid AgentId, string ProductName, DateTimeOffset? TravelDate, DateTimeOffset? ReturnDate,
    int PaxCount, string? SpecialRequests, AgentQuoteStatus Status, decimal? QuotedAmount, string? QuotedNote);

/// <summary>Đại lý gửi yêu cầu báo giá.</summary>
public sealed record CreateAgentQuoteRequestDto(
    Guid AgentId, string ProductName, DateTimeOffset? TravelDate, DateTimeOffset? ReturnDate,
    int PaxCount, string? SpecialRequests);

/// <summary>Sales chào giá (điền số tiền + ghi chú) → chuyển Quoted.</summary>
public sealed record QuoteAgentRequestDto(decimal QuotedAmount, string? QuotedNote);
