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
            new("w-social", "Mạng Nội Bộ", To: "#", Perm: "post.view"),
            new("w-workspace", "Bàn làm việc", To: "#", Perm: "report.dashboard.view"),
            new("w-dashboard", "Tổng quan", To: "/Dashboard", Perm: "report.dashboard.view"),
            new("w-noti", "Thông báo", To: "/Notifications", Perm: "report.dashboard.view"),
        }),
        new("g-provider", "Nhà cung cấp", "ti ti-building-store", Children: new List<MenuNode>
        {
            new("p-all", "Tất cả Nhà cung cấp", To: "/Providers", Perm: "provider.view"),
            new("p-services", "Danh mục dịch vụ", To: "/ServiceItems", Perm: "service.view"),
            new("p-pricing", "Bảng giá NCC", To: "/ProviderServices", Perm: "service.view"),
            new("p-terms", "Điều khoản TT NCC", To: "/PaymentTerms", Perm: "provider.view"),
            new("p-series", "Series Vé / Quỹ vé", To: "#", Perm: "ticketfund.view"),
        }),
        new("g-crm", "CRM", "ti ti-users", Children: new List<MenuNode>
        {
            new("crm-share", "Chia số Sale", To: "/LeadCampaigns", Perm: "lead.view"),
            new("crm-opp", "Cơ hội bán hàng", To: "/Leads", Perm: "lead.view"),
            new("crm-data", "Data khách hàng", To: "/Customers", Perm: "customer.view"),
            new("crm-dedup", "Rà khách trùng", To: "/Customers/Duplicates", Perm: "customer.view"),
            new("crm-care", "Quản lý lịch hẹn", To: "/CustomerCares", Perm: "care.view"),
            new("crm-feedback", "Feedback", Children: new List<MenuNode>
            {
                new("fb-general", "Feedback chung", To: "/TourRatings", Perm: "rating.view"),
                new("fb-tour", "Feedback theo Tour", To: "/TourRatings", Perm: "rating.view"),
                new("fb-zns", "Feedback ZNS", To: "#", Perm: "rating.view"),
            }),
        }),
        new("g-quote", "Báo Giá", "ti ti-calculator", Children: new List<MenuNode>
        {
            new("q-tour", "Tính giá Tour", To: "#", Perm: "quote.view"),
            new("q-combo", "Tính giá Combo", To: "#", Perm: "quote.view"),
            new("q-git", "Tour GIT/Combo", To: "#", Perm: "quote.view"),
            new("q-landtour", "Landtour", To: "#", Perm: "quote.view"),
            new("q-booking", "Booking Phòng", To: "#", Perm: "quote.view"),
            new("q-service", "Dịch vụ lẻ", To: "#", Perm: "quote.view"),
            new("q-visa", "Visa", To: "#", Perm: "quote.view"),
            new("q-agent", "Báo giá Đại lý (B2B)", To: "/AgentQuotes", Perm: "agentquote.view"),
        }),
        new("g-order", "Đơn hàng/LKH", "ti ti-shopping-cart", Children: new List<MenuNode>
        {
            new("o-all", "Tất cả đơn hàng", To: "/Orders", Perm: "booking.view"),
            new("o-tours", "Tất cả Tour/LKH", To: "/Departures", Perm: "departure.view"),
            new("o-fit", "Tour FIT", To: "#", Perm: "booking.view"),
            new("o-git", "Tour GIT/Combo", To: "#", Perm: "booking.view"),
            new("o-landtour", "LandTour", To: "#", Perm: "booking.view"),
            new("o-visa", "Visa", To: "#", Perm: "booking.view"),
            new("o-service", "Dịch vụ lẻ", To: "#", Perm: "booking.view"),
        }),
        new("g-booking", "Booking Phòng/Khách sạn", "ti ti-building-skyscraper", Children: new List<MenuNode>
        {
            new("b-roomfund", "Quỹ phòng", To: "#", Perm: "roomfund.view"),
            new("b-list", "Danh sách Booking", To: "/ServiceBookings", Perm: "servicebooking.view"),
            new("b-roomclass", "Hạng phòng (danh mục)", To: "/RoomClasses", Perm: "servicebooking.view"),
        }),
        new("g-flight", "Vé Máy Bay", "ti ti-plane", Children: new List<MenuNode>
        {
            new("f-provider", "Nhà cung cấp vé", To: "/Providers", Perm: "provider.view"),
            new("f-group", "Vé máy bay đoàn", To: "/FlightTickets", Perm: "ticketfund.view"),
            new("f-individual", "Vé máy bay lẻ", To: "/FlightTicketsIndividual", Perm: "ticketfund.view"),
        }),
        new("g-guide", "Hướng dẫn viên", "ti ti-id-badge-2", Children: new List<MenuNode>
        {
            new("gd-provider", "Hướng dẫn viên", To: "/Providers", Perm: "guide.view"),
            new("gd-calendar", "Lịch điều Hướng dẫn viên", To: "#", Perm: "guide.view"),
            new("gd-report", "Báo cáo", To: "#", Perm: "guide.view"),
        }),
        new("g-vehicle", "Quản lý xe", "ti ti-car", Children: new List<MenuNode>
        {
            new("v-store", "Kho xe", To: "/Vehicles", Perm: "vehicle.view"),
            new("v-waiting", "Lịch xe chờ duyệt", To: "#", Perm: "vehicle.view"),
            new("v-manage", "Lịch điều xe", To: "#", Perm: "vehicle.view"),
            new("v-report", "Báo cáo", To: "#", Perm: "vehicle.view"),
        }),
        new("g-operation", "Điều hành Tour", "ti ti-clipboard-list", Children: new List<MenuNode>
        {
            new("op-voucher", "Phiếu điều hành dịch vụ", To: "#", Perm: "servicebooking.view"),
            new("op-calendar", "Lịch điều hành", To: "#", Perm: "departure.view"),
        }),
        new("g-finance", "Tài chính/Kế toán", "ti ti-building-bank", Children: new List<MenuNode>
        {
            new("fi-waiting", "Phiếu thu chờ", To: "#", Perm: "receipt.view"),
            new("fi-receipt", "Phiếu thu", To: "/Receipts", Perm: "receipt.view"),
            new("fi-payment", "Phiếu chi", To: "/Payments", Perm: "payment.view"),
            new("fi-invoice", "Danh sách hoá đơn (VAT)", To: "/Invoices", Perm: "invoice.view"),
            new("fi-cashflow", "Thống kê dòng tiền", To: "/CashFlowReport", Perm: "report.cashflow.view"),
            new("fi-debt-c", "Công nợ khách", To: "/CustomerDebtReport", Perm: "report.debt.view"),
            new("fi-debt-p", "Công nợ NCC", To: "/ProviderDebtReport", Perm: "report.providerdebt.view"),
        }),
        new("g-kpi", "KPIs", "ti ti-trending-up", Children: new List<MenuNode>
        {
            new("kpi-config", "Thiết lập KPIs", To: "/KpiConfig", Perm: "report.dashboard.view"),
        }),
        new("g-commission", "Hoa Hồng", "ti ti-percentage", Children: new List<MenuNode>
        {
            new("hh-config", "Thiết lập hoa hồng", To: "/CommissionConfig", Perm: "commission.view"),
            new("hh-campaign", "Chính sách hoa hồng (bậc thang)", To: "/CommissionTiers", Perm: "commission.view"),
            new("hh-customer", "HH theo loại khách", To: "/CustomerCommissionRules", Perm: "commission.view"),
            new("hh-source", "Báo cáo theo nguồn", To: "/CommissionBySourceReport", Perm: "report.commission.view"),
            new("hh-milestone", "Báo cáo theo cột mốc", To: "/CommissionByMilestoneReport", Perm: "report.commission.view"),
        }),
        new("g-project", "Dự án & Công việc", "ti ti-checklist", Children: new List<MenuNode>
        {
            new("pj-project", "Dự án", To: "/Workflows", Perm: "workflow.view"),
            new("pj-mytask", "Công việc của tôi", To: "/WorkTasks", Perm: "task.view"),
            new("pj-tasks", "Danh sách Công việc", To: "/WorkTasks", Perm: "task.view"),
            new("pj-perf", "Báo cáo Hiệu suất", To: "#", Perm: "task.view"),
        }),
        new("g-marketing", "Marketing", "ti ti-speakerphone", Children: new List<MenuNode>
        {
            new("mkt-email", "Email Marketing", Children: new List<MenuNode>
            {
                new("mkt-campaign", "Chiến dịch", To: "/MarketingCampaigns", Perm: "marketing.view"),
                new("mkt-store", "Kho Email Mẫu", To: "/MessageTemplates", Perm: "marketing.view"),
            }),
            new("mkt-zalo", "Zalo OA/ZBS", Children: new List<MenuNode>
            {
                new("zalo-oa", "Thông tin OA", To: "#", Perm: "marketing.view"),
                new("zalo-zns", "ZNS", To: "#", Perm: "marketing.view"),
                new("zalo-uid", "Zalo UID (Tin follow OA)", To: "#", Perm: "marketing.view"),
            }),
            new("mkt-posts", "Bài viết", To: "/Posts", Perm: "post.view"),
            new("mkt-postcat", "Chuyên mục bài viết", To: "/PostCategories", Perm: "post.view"),
        }),
        new("g-report", "Báo cáo", "ti ti-chart-bar", Children: new List<MenuNode>
        {
            new("rp-seller", "Nhân viên", To: "/SellerReport", Perm: "report.turnover.view"),
            new("rp-money", "Tài chính", To: "/FinanceReport", Perm: "report.turnover.view"),
            new("rp-tourtype", "Thu chi theo loại tour", To: "/TourTypeReport", Perm: "report.turnover.view"),
            new("rp-export", "Xuất báo cáo", To: "#", Perm: "report.turnover.view"),
            new("rp-system", "Báo cáo tổng hợp", To: "#", Perm: "report.turnover.view"),
        }),
        new("g-agent", "Đại lý (B2B)", "ti ti-heart-handshake", Children: new List<MenuNode>
        {
            new("ag-list", "Danh sách đại lý", To: "/Agents", Perm: "agent.view"),
            new("ag-booking", "Đặt chỗ đại lý", To: "/AgentBookings", Perm: "agentquote.view"),
        }),
        new("g-system", "Cài đặt hệ thống", "ti ti-settings", Children: new List<MenuNode>
        {
            new("sys-users", "Thành viên", To: "/Users", Perm: "user.view"),
            new("sys-roles", "Vai trò & quyền", To: "/Roles", Perm: "user.view"),
            new("sys-config", "Cấu hình", To: "/ConfigHub", Perm: "user.view"),
            new("sys-billing", "Gói dịch vụ", To: "/Billing", Perm: "subscription.view"),
        }),
        new("g-log", "Log hệ thống", "ti ti-history", Children: new List<MenuNode>
        {
            new("log-system", "Log hệ thống", To: "/ActivityLogs", Perm: "activitylog.view"),
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
