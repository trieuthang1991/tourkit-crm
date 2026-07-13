namespace TourKit.Application.B2B.Dtos;

public sealed record AgentDto(
    Guid Id, string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);

/// <summary>Bộ lọc danh sách đại lý (bám hệ cũ): từ khoá (mã/tên/liên hệ) · trạng thái (0 ngừng, 1 hoạt động).</summary>
public sealed record AgentListFilter(string? Q = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Đại lý: tổng + hoạt động + ngừng + tổng hạn mức.</summary>
public sealed record AgentStatsDto(int Total, int Active, int Inactive, decimal TotalCreditLimit);

public sealed record CreateAgentDto(
    string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);

public sealed record UpdateAgentDto(
    string Code, string Name, string? ContactPerson, string? Phone, string? Email,
    string? TaxCode, string? Address, decimal CreditLimit, int Status);
