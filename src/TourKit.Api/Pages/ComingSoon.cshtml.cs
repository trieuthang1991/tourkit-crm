using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages;

// Trang giữ chỗ TRUNG THỰC cho các tính năng cần tích hợp ngoài / quyết định sản phẩm
// (Zalo OA·ZNS·UID, Feedback ZNS, Mạng nội bộ, Xuất báo cáo tổng). KHÔNG bịa dữ liệu.
[Authorize]
public class ComingSoonModel : PageModel
{
    [BindProperty(SupportsGet = true, Name = "f")] public string? Feature { get; set; }

    public string Title { get; private set; } = "Tính năng đang phát triển";
    public string Note { get; private set; } = "Tính năng này đang được hoàn thiện.";
    public string Icon { get; private set; } = "ti-tool";

    private static readonly Dictionary<string, (string Title, string Note, string Icon)> Map = new()
    {
        ["social"] = ("Mạng Nội Bộ", "Bảng tin nội bộ (đăng bài, tương tác giữa nhân viên) đang được xây dựng.", "ti-users-group"),
        ["zalo-oa"] = ("Zalo OA — Thông tin OA", "Cần kết nối tài khoản Zalo Official Account. Vào Cấu hình để nhập App ID / secret trước khi dùng.", "ti-brand-zalo"),
        ["zalo-zns"] = ("Zalo ZNS", "Gửi tin ZNS (Zalo Notification Service) cần kết nối Zalo OA và template ZNS đã duyệt.", "ti-message-2"),
        ["zalo-uid"] = ("Zalo UID — Tin follow OA", "Đồng bộ UID người theo dõi OA cần kết nối Zalo OA.", "ti-user-search"),
        ["fb-zns"] = ("Feedback ZNS", "Thu thập phản hồi qua ZNS cần kết nối Zalo OA và template khảo sát.", "ti-star"),
        ["export"] = ("Xuất báo cáo", "Trung tâm xuất báo cáo tổng hợp đang được hoàn thiện. Hiện có thể xuất danh sách Khách hàng ở màn Khách hàng.", "ti-file-export"),
    };

    public void OnGet()
    {
        if (Feature is not null && Map.TryGetValue(Feature, out var m))
        {
            Title = m.Title;
            Note = m.Note;
            Icon = m.Icon;
        }
    }
}
