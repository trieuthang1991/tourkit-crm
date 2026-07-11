namespace TourKit.Application.Commission.Dtos;

/// <summary>DTO quy tắc hoa hồng theo loại khách — trả ra cho client.</summary>
public sealed record CustomerCommissionRuleDto(Guid Id, int CustomerType, decimal Percentage, int Status);

public sealed record CreateCustomerCommissionRuleDto(int CustomerType, decimal Percentage, int Status);

public sealed record UpdateCustomerCommissionRuleDto(decimal Percentage, int Status);
