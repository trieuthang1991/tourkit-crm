namespace TourKit.Application.Commission.Dtos;

/// <summary>DTO quy tắc hoa hồng theo loại khách — trả ra cho client. CustomerTypeName enrich từ danh mục loại khách.</summary>
public sealed record CustomerCommissionRuleDto(Guid Id, int CustomerType, decimal Percentage, int Status, string? CustomerTypeName = null);

/// <summary>Bộ lọc HH theo loại khách: loại khách (Code) · trạng thái. Status: 1 áp dụng, 0 tạm ngừng.</summary>
public sealed record CustomerCommissionRuleListFilter(int? CustomerType = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn HH theo loại khách: tổng · đang áp dụng · tạm ngừng · tỉ lệ trung bình.</summary>
public sealed record CustomerCommissionRuleStatsDto(int Total, int Active, int Inactive, decimal AvgPercentage);

public sealed record CreateCustomerCommissionRuleDto(int CustomerType, decimal Percentage, int Status);

public sealed record UpdateCustomerCommissionRuleDto(decimal Percentage, int Status);
