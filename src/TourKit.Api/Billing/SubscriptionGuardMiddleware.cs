using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;

using TourKit.Shared.Enums;

namespace TourKit.Api.Billing;

/// <summary>
/// Chặn request nghiệp vụ nếu subscription của tenant đã hết hạn/huỷ.
/// Miễn trừ: auth, đăng ký, xem gói, xem/đổi subscription (để còn gia hạn).
/// Tenant CHƯA có subscription → cho qua (ân hạn) — chỉ chặn khi CÓ mà không active.
/// </summary>
public sealed class SubscriptionGuardMiddleware
{
    private static readonly string[] ExemptPrefixes =
    [
        "/api/v1/auth", "/api/v1/registration", "/api/v1/plans", "/api/v1/subscription",
    ];

    private readonly RequestDelegate _next;

    public SubscriptionGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AmbientTenantContext tenant, AppDbContext db)
    {
        if (!tenant.HasTenant || IsExempt(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sub = await db.Subscriptions.AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (sub is not null && !IsActive(sub))
        {
            await Results.Problem(
                statusCode: StatusCodes.Status402PaymentRequired,
                title: "Gói dịch vụ đã hết hạn hoặc bị huỷ. Vui lòng gia hạn.")
                .ExecuteAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsActive(Subscription s) =>
        s.Status == SubscriptionStatus.Active
        && (s.ExpiresAt is null || s.ExpiresAt > DateTimeOffset.UtcNow);

    private static bool IsExempt(PathString path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
