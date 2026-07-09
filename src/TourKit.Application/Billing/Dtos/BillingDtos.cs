using TourKit.Shared.Enums;

namespace TourKit.Application.Billing.Dtos;

/// <summary>DTO gói dịch vụ trả ra cho client (không lộ entity).</summary>
public sealed record PlanDto(Guid Id, string Code, string Name, int MaxUsers, int MaxTours, decimal PriceMonthly);

/// <summary>DTO subscription hiện tại của tenant trả ra cho client.</summary>
public sealed record SubscriptionDto(
    Guid Id, Guid PlanId, string PlanCode, SubscriptionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? ExpiresAt);

/// <summary>DTO đổi gói.</summary>
public sealed record ChangePlanDto(string PlanCode);
