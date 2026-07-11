using Hangfire.Dashboard;

namespace TourKit.Api.BackgroundJobs;

/// <summary>Chỉ user đã xác thực mới xem được dashboard Hangfire tại /hangfire.</summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
        => context.GetHttpContext().User.Identity?.IsAuthenticated == true;
}
