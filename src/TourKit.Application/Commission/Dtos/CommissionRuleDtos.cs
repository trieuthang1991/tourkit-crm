namespace TourKit.Application.Commission.Dtos;

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record CommissionRuleDto(Guid Id, Guid UserId, decimal Percentage, int Status);

/// <summary>DTO tạo quy tắc hoa hồng mới cho 1 user.</summary>
public sealed record CreateCommissionRuleDto(Guid UserId, decimal Percentage, int Status);

/// <summary>DTO cập nhật quy tắc hoa hồng.</summary>
public sealed record UpdateCommissionRuleDto(decimal Percentage, int Status);
