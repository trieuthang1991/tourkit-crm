namespace TourKit.Api.Routing;

/// <summary>
/// Chuyển hướng 301 từ URL cũ (PascalCase, vd /Orders, /Customers/Details/{id}) sang route tiếng
/// Việt mới (/don-hang, /khach-hang/{id}). Nhờ vậy bookmark, link đã gửi đi, ảnh chụp màn hình cũ
/// không chết khi đổi đường dẫn.
///
/// Chạy TRƯỚC routing. Chỉ động vào GET/HEAD — không đụng POST (redirect POST sẽ nuốt mất dữ liệu
/// form). Khớp theo ĐOẠN đường dẫn (segment) để "/Orders" không vô tình khớp "/OrdersXyz", và giữ
/// nguyên phần đuôi động (/{id}) cùng query string.
/// </summary>
public sealed class LegacyRouteRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public LegacyRouteRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) && TryMapLegacy(path, out var target))
            {
                // 301 vĩnh viễn + giữ query string. Trình duyệt/công cụ tìm kiếm nhớ luôn địa chỉ mới.
                context.Response.Redirect(target + context.Request.QueryString, permanent: true);
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Tìm khoá cũ DÀI NHẤT là tiền tố (theo đoạn) của path, thay bằng route mới, giữ nguyên đuôi.
    /// "/Orders/Detail/abc" → khớp "/Orders/Detail" (dài hơn "/Orders") → "/don-hang/abc".
    /// </summary>
    private static bool TryMapLegacy(string path, out string target)
    {
        string? bestKey = null;
        string? bestValue = null;
        foreach (var (key, value) in RouteMap.LegacyToFriendly)
        {
            var isPrefix = path.Equals(key, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase);
            if (isPrefix && (bestKey is null || key.Length > bestKey.Length))
            {
                bestKey = key;
                bestValue = value;
            }
        }

        if (bestKey is null)
        {
            target = string.Empty;
            return false;
        }

        var tail = path[bestKey.Length..];   // phần đuôi động, gồm cả dấu "/" đầu nếu có
        target = bestValue + tail;
        return true;
    }
}
