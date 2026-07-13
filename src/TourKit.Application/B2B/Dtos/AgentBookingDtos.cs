namespace TourKit.Application.B2B.Dtos;

public sealed record AgentPassengerDto(
    Guid Id, string FullName, DateTimeOffset? DateOfBirth, string? PassportNo, string? Nationality, string? Note);

public sealed record AgentBookingDto(
    Guid Id, Guid AgentId, Guid QuoteRequestId, string Code, decimal TotalAmount, int Status, string? Note,
    AgentPassengerDto[] Passengers);

public sealed record AgentBookingSummaryDto(
    Guid Id, Guid AgentId, Guid QuoteRequestId, string Code, decimal TotalAmount, int Status,
    string? AgentName = null);

/// <summary>Bộ lọc đặt chỗ đại lý (bám hệ cũ): đại lý · trạng thái · từ khoá (mã). Status: 0 chờ,1 xác nhận,2 huỷ,3 hoàn tất.</summary>
public sealed record AgentBookingListFilter(string? Q = null, Guid? AgentId = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Đặt chỗ đại lý: tổng + theo trạng thái + tổng tiền.</summary>
public sealed record AgentBookingStatsDto(int Total, int Pending, int Confirmed, int Cancelled, int Done, decimal TotalAmount);

/// <summary>Tạo Booking từ một yêu cầu báo giá đã Confirmed.</summary>
public sealed record CreateAgentBookingDto(Guid QuoteRequestId, string Code, string? Note);

public sealed record AddAgentPassengerDto(
    string FullName, DateTimeOffset? DateOfBirth, string? PassportNo, string? Nationality, string? Note);
