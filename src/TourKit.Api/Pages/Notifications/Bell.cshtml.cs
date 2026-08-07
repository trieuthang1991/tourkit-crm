using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Notifications;

namespace TourKit.Api.Pages.Notifications;

/// <summary>
/// Nguồn dữ liệu cho chuông thông báo trên navbar. Navbar là partial dùng chung mọi trang nên không
/// nạp được qua model của từng trang — phải có một handler toàn cục gọi bằng AJAX.
///
/// Chỉ yêu cầu ĐĂNG NHẬP (khác màn /thong-bao đòi quyền report.dashboard.view): thông báo là của
/// chính người dùng, ai đăng nhập cũng phải nhận được việc giao cho mình.
/// </summary>
[Authorize]
public class BellModel : PageModel
{
    /// <summary>Số dòng hiện trong dropdown — đủ để liếc nhanh, xem đầy đủ thì sang /thong-bao.</summary>
    private const int Take = 8;

    private readonly INotificationService _svc;
    public BellModel(INotificationService svc) => _svc = svc;

    /// <summary>GET /thong-bao/chuong — trả về số chưa đọc + N thông báo gần nhất.</summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var items = await _svc.ListMineAsync(false, Take);
        var unread = await _svc.UnreadCountAsync();
        return new JsonResult(new
        {
            unread,
            items = items.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                linkUrl = n.LinkUrl,
                type = n.Type,
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
            }).ToList(),
        });
    }

    public async Task<IActionResult> OnPostReadAsync(Guid id)
    {
        await _svc.MarkReadAsync(id);
        return new JsonResult(Result.Success(null, new { unread = await _svc.UnreadCountAsync() }));
    }

    public async Task<IActionResult> OnPostReadAllAsync()
    {
        await _svc.MarkAllReadAsync();
        return new JsonResult(Result.Success("Đã đánh dấu tất cả đã đọc.", new { unread = 0 }));
    }
}
