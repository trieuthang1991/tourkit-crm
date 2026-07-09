using TourKit.Shared.Entities;

namespace TourKit.Api.Billing;

/// <summary>DTO gói dịch vụ trả ra cho client (không lộ entity).</summary>
public sealed record PlanResponse(Guid Id, string Code, string Name, int MaxUsers, int MaxTours, decimal PriceMonthly);

/// <summary>DTO subscription hiện tại của tenant trả ra cho client.</summary>
public sealed record SubscriptionResponse(
    Guid Id, Guid PlanId, string PlanCode, SubscriptionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? ExpiresAt);

/// <summary>DTO đổi gói.</summary>
public sealed record ChangePlanRequest(string PlanCode);
