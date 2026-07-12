using TourKit.Shared.Enums;

namespace TourKit.Application.Providers.Dtos;

public sealed record ProviderDto(
    Guid Id, string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status,
    // Công nợ NCC (bám danh sách hệ cũ): tổng mua (OrderCost) · đã trả (phiếu chi đã duyệt) · còn nợ. Chỉ ở danh sách.
    decimal TotalCost = 0m, decimal Paid = 0m, decimal Outstanding = 0m);

public sealed record CreateProviderDto(
    string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status);

public sealed record UpdateProviderDto(
    string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, Guid? PaymentTermId, int Rate, int Status);

/// <summary>Bộ lọc danh sách NCC (bám hệ cũ). Tất cả optional.</summary>
public sealed record ProviderListFilter(string? Q = null, int? Type = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn NCC: tổng + đang hoạt động + ngừng.</summary>
public sealed record ProviderStatsDto(int Total, int Active, int Inactive);
