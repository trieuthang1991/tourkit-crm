namespace TourKit.Application.B2B.Dtos;

public sealed record AgentPassengerDto(
    Guid Id, string FullName, DateTimeOffset? DateOfBirth, string? PassportNo, string? Nationality, string? Note);

public sealed record AgentBookingDto(
    Guid Id, Guid AgentId, Guid QuoteRequestId, string Code, decimal TotalAmount, int Status, string? Note,
    AgentPassengerDto[] Passengers);

public sealed record AgentBookingSummaryDto(
    Guid Id, Guid AgentId, Guid QuoteRequestId, string Code, decimal TotalAmount, int Status);

/// <summary>Tạo Booking từ một yêu cầu báo giá đã Confirmed.</summary>
public sealed record CreateAgentBookingDto(Guid QuoteRequestId, string Code, string? Note);

public sealed record AddAgentPassengerDto(
    string FullName, DateTimeOffset? DateOfBirth, string? PassportNo, string? Nationality, string? Note);
