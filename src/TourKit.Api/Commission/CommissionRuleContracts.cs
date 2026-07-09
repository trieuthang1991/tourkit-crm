namespace TourKit.Api.Commission;

/// <summary>DTO tạo quy tắc hoa hồng mới cho 1 user.</summary>
public sealed record CreateCommissionRuleRequest(Guid UserId, decimal Percentage, int Status);

/// <summary>DTO cập nhật quy tắc hoa hồng.</summary>
public sealed record UpdateCommissionRuleRequest(decimal Percentage, int Status);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record CommissionRuleResponse(Guid Id, Guid UserId, decimal Percentage, int Status);
