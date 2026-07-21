using Hangfire.Dashboard;

namespace TourKit.Api.BackgroundJobs;

/// <summary>
/// Bảng điều khiển Hangfire tại /hangfire.
///
/// Chỉ "đã đăng nhập" là KHÔNG đủ: các job nhắc việc quét XUYÊN TENANT (IgnoreQueryFilters), nên
/// tham số job và stack trace lỗi hiện trên bảng này để lộ dữ liệu của mọi đơn vị. Bảng còn cho
/// xếp lại/xoá job định kỳ — gồm cả job nhả chỗ giữ, tức đụng thẳng vào vận hành đặt chỗ.
/// Vì vậy phải đòi quyền quản trị job, không phải chỉ đăng nhập.
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Dùng lại quyền quản trị người dùng thay vì đặt mã quyền mới: mã mới phải seed và gán vào vai
    /// trò, chưa gán xong thì KHÔNG AI vào được /hangfire, kể cả quản trị. Ai quản trị được người
    /// dùng thì đã là vai trò cao nhất trong đơn vị.
    /// </summary>
    public const string RequiredPermission = "user.manage";

    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.HasClaim("perm", RequiredPermission);
    }
}
