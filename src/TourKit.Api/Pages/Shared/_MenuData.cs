namespace TourKit.Api.Pages.Shared;

public sealed record MenuNode(
    string Key, string Label, string? Icon = null, string? Perm = null,
    string? To = null, IReadOnlyList<MenuNode>? Children = null);

public static class MenuData
{
    // Bám CHÍNH XÁC menu hệ cũ (web/src/app/AppShell.tsx — 19 nhóm, nhãn/thứ tự/lồng/perm).
    // Icon: Tabler (ti ti-*) khớp bộ icon Vuexy đang nạp. To = route thực tế đợt 1
    // (chỉ /Customers có trang thật; phần còn lại "#" placeholder, nối dần đợt sau).
    public static readonly IReadOnlyList<MenuNode> Groups = new List<MenuNode>
    {
        new("g-workspace", "Workspace", "ti ti-layout-dashboard", Children: new List<MenuNode>
        {
            new("w-social", "Mạng Nội Bộ", To: "/ComingSoon?f=social", Perm: "post.view"),
            new("w-workspace", "Bàn làm việc", To: "/ban-lam-viec", Perm: "report.dashboard.view"),
            new("w-dashboard", "Tổng quan", To: "/tong-quan", Perm: "report.dashboard.view"),
            new("w-noti", "Thông báo", To: "/thong-bao", Perm: "report.dashboard.view"),
        }),
        new("g-provider", "Nhà cung cấp", "ti ti-building-store", Children: new List<MenuNode>
        {
            new("p-all", "Tất cả Nhà cung cấp", To: "/nha-cung-cap", Perm: "provider.view"),
            new("p-services", "Danh mục dịch vụ", To: "/danh-muc-dich-vu", Perm: "service.view"),
            new("p-pricing", "Bảng giá NCC", To: "/bang-gia-ncc", Perm: "service.view"),
            new("p-terms", "Điều khoản TT NCC", To: "/dieu-khoan-thanh-toan", Perm: "provider.view"),
            new("p-series", "Series Vé / Quỹ vé", To: "/quy-ve", Perm: "ticketfund.view"),
        }),
        new("g-crm", "CRM", "ti ti-users", Children: new List<MenuNode>
        {
            new("crm-share", "Chia số Sale", To: "/chia-so-sale", Perm: "lead.view"),
            new("crm-opp", "Cơ hội bán hàng", To: "/co-hoi", Perm: "lead.view"),
            new("crm-data", "Data khách hàng", To: "/khach-hang", Perm: "customer.view"),
            new("crm-dedup", "Rà khách trùng", To: "/khach-hang/trung-lap", Perm: "customer.view"),
            new("crm-care", "Quản lý lịch hẹn", To: "/lich-hen", Perm: "care.view"),
            new("crm-feedback", "Feedback", Children: new List<MenuNode>
            {
                new("fb-general", "Feedback chung", To: "/danh-gia", Perm: "rating.view"),
                new("fb-tour", "Feedback theo Tour", To: "/danh-gia/theo-tour", Perm: "rating.view"),
                new("fb-zns", "Feedback ZNS", To: "/ComingSoon?f=fb-zns", Perm: "rating.view"),
            }),
        }),
        new("g-quote", "Báo Giá", "ti ti-calculator", Children: new List<MenuNode>
        {
            new("q-tour", "Tính giá Tour", To: "/bao-gia", Perm: "quote.view"),
            new("q-combo", "Tính giá Combo", To: "/bao-gia/loai/combo", Perm: "quote.view"),
            new("q-git", "Tour GIT/Combo", To: "/bao-gia/loai/git", Perm: "quote.view"),
            new("q-landtour", "Landtour", To: "/bao-gia/loai/landtour", Perm: "quote.view"),
            new("q-booking", "Booking Phòng", To: "/bao-gia/loai/booking", Perm: "quote.view"),
            new("q-service", "Dịch vụ lẻ", To: "/bao-gia/loai/dich-vu-le", Perm: "quote.view"),
            new("q-visa", "Visa", To: "/bao-gia/loai/visa", Perm: "quote.view"),
            new("q-agent", "Báo giá Đại lý (B2B)", To: "/bao-gia-dai-ly", Perm: "agentquote.view"),
        }),
        new("g-order", "Đơn hàng/LKH", "ti ti-shopping-cart", Children: new List<MenuNode>
        {
            new("o-all", "Tất cả đơn hàng", To: "/don-hang", Perm: "booking.view"),
            new("o-tours", "Tất cả Tour/LKH", To: "/chuyen-di", Perm: "departure.view"),
            new("o-fit", "Tour FIT", To: "/chuyen-di/loai/tour-fit", Perm: "departure.view"),
            new("o-git", "Tour GIT/Combo", To: "/chuyen-di/loai/git", Perm: "departure.view"),
            new("o-landtour", "LandTour", To: "/chuyen-di/loai/landtour", Perm: "departure.view"),
            new("o-visa", "Visa", To: "/don-hang/loai/visa", Perm: "booking.view"),
            new("o-service", "Dịch vụ lẻ", To: "/don-hang/loai/dich-vu-le", Perm: "booking.view"),
        }),
        new("g-booking", "Booking Phòng/Khách sạn", "ti ti-building-skyscraper", Children: new List<MenuNode>
        {
            new("b-roomfund", "Quỹ phòng", To: "/quy-phong", Perm: "roomfund.view"),
            new("b-list", "Danh sách Booking", To: "/booking-dich-vu", Perm: "servicebooking.view"),
            new("b-roomclass", "Hạng phòng (danh mục)", To: "/hang-phong", Perm: "servicebooking.view"),
        }),
        new("g-flight", "Vé Máy Bay", "ti ti-plane", Children: new List<MenuNode>
        {
            new("f-provider", "Nhà cung cấp vé", To: "/nha-cung-cap/loai/ve", Perm: "provider.view"),
            new("f-group", "Vé máy bay đoàn", To: "/ve-may-bay-doan", Perm: "ticketfund.view"),
            new("f-individual", "Vé máy bay lẻ", To: "/ve-may-bay-le", Perm: "ticketfund.view"),
        }),
        new("g-guide", "Hướng dẫn viên", "ti ti-id-badge-2", Children: new List<MenuNode>
        {
            new("gd-provider", "Hướng dẫn viên", To: "/nha-cung-cap/loai/hdv", Perm: "guide.view"),
            new("gd-calendar", "Lịch điều Hướng dẫn viên", To: "/dieu-hdv", Perm: "guide.view"),
            new("gd-report", "Báo cáo", To: "/bao-cao-hdv", Perm: "guide.view"),
        }),
        new("g-vehicle", "Quản lý xe", "ti ti-car", Children: new List<MenuNode>
        {
            new("v-store", "Kho xe", To: "/kho-xe", Perm: "vehicle.view"),
            new("v-waiting", "Lịch xe chờ duyệt", To: "/lich-xe-cho", Perm: "vehicle.view"),
            new("v-manage", "Lịch điều xe", To: "/dieu-xe", Perm: "vehicle.view"),
            new("v-report", "Báo cáo", To: "/bao-cao-xe", Perm: "vehicle.view"),
        }),
        new("g-operation", "Điều hành Tour", "ti ti-clipboard-list", Children: new List<MenuNode>
        {
            new("op-voucher", "Phiếu điều hành dịch vụ", To: "/phieu-dieu-hanh", Perm: "servicebooking.view"),
            new("op-calendar", "Lịch điều hành", To: "/lich-dieu-hanh", Perm: "departure.view"),
        }),
        new("g-finance", "Tài chính/Kế toán", "ti ti-building-bank", Children: new List<MenuNode>
        {
            new("fi-waiting", "Phiếu thu chờ", To: "/phieu-thu-cho", Perm: "receipt.view"),
            new("fi-receipt", "Phiếu thu", To: "/phieu-thu", Perm: "receipt.view"),
            new("fi-payment", "Phiếu chi", To: "/phieu-chi", Perm: "payment.view"),
            new("fi-invoice", "Danh sách hoá đơn (VAT)", To: "/hoa-don", Perm: "invoice.view"),
            new("fi-cashflow", "Thống kê dòng tiền", To: "/dong-tien", Perm: "report.cashflow.view"),
            new("fi-debt-c", "Công nợ khách", To: "/cong-no-khach", Perm: "report.debt.view"),
            new("fi-debt-p", "Công nợ NCC", To: "/cong-no-ncc", Perm: "report.providerdebt.view"),
        }),
        new("g-kpi", "KPIs", "ti ti-trending-up", Children: new List<MenuNode>
        {
            new("kpi-config", "Thiết lập KPIs", To: "/thiet-lap-kpi", Perm: "report.dashboard.view"),
        }),
        new("g-commission", "Hoa Hồng", "ti ti-percentage", Children: new List<MenuNode>
        {
            new("hh-config", "Thiết lập hoa hồng", To: "/thiet-lap-hoa-hong", Perm: "commission.view"),
            new("hh-campaign", "Chính sách hoa hồng (bậc thang)", To: "/chinh-sach-hoa-hong", Perm: "commission.view"),
            new("hh-customer", "HH theo loại khách", To: "/hoa-hong-loai-khach", Perm: "commission.view"),
            new("hh-source", "Báo cáo theo nguồn", To: "/hoa-hong-theo-nguon", Perm: "report.commission.view"),
            new("hh-milestone", "Báo cáo theo cột mốc", To: "/hoa-hong-theo-moc", Perm: "report.commission.view"),
        }),
        new("g-project", "Dự án & Công việc", "ti ti-checklist", Children: new List<MenuNode>
        {
            new("pj-project", "Dự án", To: "/du-an", Perm: "workflow.view"),
            new("pj-mytask", "Công việc của tôi", To: "/cong-viec/cua-toi", Perm: "task.view"),
            new("pj-tasks", "Danh sách Công việc", To: "/cong-viec", Perm: "task.view"),
            new("pj-perf", "Báo cáo Hiệu suất", To: "/hieu-suat-cong-viec", Perm: "task.view"),
        }),
        new("g-marketing", "Marketing", "ti ti-speakerphone", Children: new List<MenuNode>
        {
            new("mkt-email", "Email Marketing", Children: new List<MenuNode>
            {
                new("mkt-campaign", "Chiến dịch", To: "/chien-dich", Perm: "marketing.view"),
                new("mkt-store", "Kho Email Mẫu", To: "/mau-tin-nhan", Perm: "marketing.view"),
            }),
            new("mkt-zalo", "Zalo OA/ZBS", Children: new List<MenuNode>
            {
                new("zalo-oa", "Thông tin OA", To: "/ComingSoon?f=zalo-oa", Perm: "marketing.view"),
                new("zalo-zns", "ZNS", To: "/ComingSoon?f=zalo-zns", Perm: "marketing.view"),
                new("zalo-uid", "Zalo UID (Tin follow OA)", To: "/ComingSoon?f=zalo-uid", Perm: "marketing.view"),
            }),
            new("mkt-posts", "Bài viết", To: "/bai-viet", Perm: "post.view"),
            new("mkt-postcat", "Chuyên mục bài viết", To: "/chuyen-muc", Perm: "post.view"),
        }),
        new("g-report", "Báo cáo", "ti ti-chart-bar", Children: new List<MenuNode>
        {
            new("rp-seller", "Nhân viên", To: "/bao-cao-nhan-vien", Perm: "report.turnover.view"),
            new("rp-money", "Tài chính", To: "/bao-cao-tai-chinh", Perm: "report.turnover.view"),
            new("rp-tourtype", "Thu chi theo loại tour", To: "/thu-chi-theo-tour", Perm: "report.turnover.view"),
            new("rp-export", "Xuất báo cáo", To: "/ComingSoon?f=export", Perm: "report.turnover.view"),
            new("rp-system", "Báo cáo tổng hợp", To: "/bao-cao-tong-hop", Perm: "report.turnover.view"),
        }),
        new("g-agent", "Đại lý (B2B)", "ti ti-heart-handshake", Children: new List<MenuNode>
        {
            new("ag-list", "Danh sách đại lý", To: "/dai-ly", Perm: "agent.view"),
            new("ag-booking", "Đặt chỗ đại lý", To: "/dat-cho-dai-ly", Perm: "agentquote.view"),
        }),
        new("g-system", "Cài đặt hệ thống", "ti ti-settings", Children: new List<MenuNode>
        {
            new("sys-users", "Thành viên", To: "/thanh-vien", Perm: "user.view"),
            new("sys-roles", "Vai trò & quyền", To: "/vai-tro", Perm: "user.view"),
            new("sys-config", "Cấu hình", To: "/cau-hinh", Perm: "user.view"),
            new("sys-billing", "Gói dịch vụ", To: "/goi-dich-vu", Perm: "subscription.view"),
        }),
        new("g-log", "Log hệ thống", "ti ti-history", Children: new List<MenuNode>
        {
            new("log-system", "Log hệ thống", To: "/nhat-ky", Perm: "activitylog.view"),
        }),
    };

    /// <summary>Lọc theo quyền: bỏ leaf thiếu perm, bỏ nhóm rỗng sau khi lọc.</summary>
    public static IReadOnlyList<MenuNode> FilterByPerm(IReadOnlyList<MenuNode> nodes, Func<string, bool> has)
    {
        var result = new List<MenuNode>();
        foreach (var n in nodes)
        {
            if (n.Children is { Count: > 0 })
            {
                var kids = FilterByPerm(n.Children, has);
                if (kids.Count > 0)
                {
                    result.Add(n with { Children = kids });
                }
            }
            else if (n.Perm is null || has(n.Perm))
            {
                result.Add(n);
            }
        }
        return result;
    }
}
