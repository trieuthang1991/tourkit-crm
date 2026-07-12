using TourKit.Shared.Enums;

namespace TourKit.Application.Crm.Dtos;

public sealed record LeadDto(
    Guid Id, string FullName, string? Phone, string? Email, string? Source,
    LeadStatus Status, Guid? AssignedToUserId, Guid? ConvertedCustomerId);

public sealed record CreateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, Guid? AssignedToUserId);

public sealed record UpdateLeadDto(
    string FullName, string? Phone, string? Email, string? Source, LeadStatus Status, Guid? AssignedToUserId);

public sealed record ConvertLeadResultDto(Guid CustomerId);

/// <summary>Bộ lọc danh sách Lead (bám hệ cũ). Tất cả optional.</summary>
public sealed record LeadListFilter(string? Q = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Lead: tổng + đếm theo trạng thái + đã chuyển KH.</summary>
public sealed record LeadStatsDto(
    int Total, int New, int Contacted, int Qualified, int Won, int Lost, int Converted);
