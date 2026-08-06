namespace TourKit.Api.Routing;

/// <summary>
/// NGUỒN DUY NHẤT ánh xạ trang Razor → route tiếng Việt thân thiện.
///
/// Vì sao tập trung một chỗ: route đứng ở 4 nơi (đăng ký trang, chuyển hướng 301 từ URL cũ,
/// menu, và link trong view). Rải rác thì lệch nhau. Đặt hết ở đây, các nơi kia đọc lại từ đây.
///
/// Quy ước: route tiếng Việt không dấu, ngăn bằng gạch nối. URL cũ dạng PascalCase (/Orders)
/// vẫn vào được nhưng bị chuyển hướng 301 sang route mới (LegacyRouteRedirectMiddleware) —
/// bookmark/link cũ không chết.
/// </summary>
public static class RouteMap
{
    /// <summary>
    /// Trang Razor ("/Thư-mục/Index") → mẫu route tiếng Việt (không có gạch đầu).
    /// Trang chi tiết/soạn kèm tham số {id} để router phân biệt với route danh sách.
    /// </summary>
    public static readonly IReadOnlyList<(string Page, string Route)> Pages = new List<(string, string)>
    {
        // --- Workspace / tổng quan ---
        ("/Workspace/Index", "ban-lam-viec"),
        ("/Dashboard/Index", "tong-quan"),
        ("/Notifications/Index", "thong-bao"),

        // --- Nhà cung cấp ---
        ("/Providers/Index", "nha-cung-cap"),
        ("/ServiceItems/Index", "danh-muc-dich-vu"),
        ("/ProviderServices/Index", "bang-gia-ncc"),
        ("/PaymentTerms/Index", "dieu-khoan-thanh-toan"),
        ("/TicketFunds/Index", "quy-ve"),

        // --- CRM ---
        ("/LeadCampaigns/Index", "chia-so-sale"),
        ("/Leads/Index", "co-hoi"),
        ("/Customers/Index", "khach-hang"),
        ("/Customers/Details", "khach-hang/{id:guid}"),
        ("/Customers/Duplicates", "khach-hang/trung-lap"),
        ("/CustomerCares/Index", "lich-hen"),
        ("/TourRatings/Index", "danh-gia"),
        ("/TourRatings/ByTour", "danh-gia/theo-tour"),

        // --- Báo giá ---
        ("/Quotes/Index", "bao-gia"),
        ("/Quotes/Edit", "bao-gia/soan/{id:guid?}"),
        ("/AgentQuotes/Index", "bao-gia-dai-ly"),

        // --- Đơn hàng / chuyến đi ---
        ("/Orders/Index", "don-hang"),
        ("/Orders/Detail", "don-hang/{id:guid}"),
        ("/Departures/Index", "chuyen-di"),
        ("/Departures/Detail", "chuyen-di/{id:guid}"),

        // --- Booking phòng ---
        ("/RoomAllotments/Index", "quy-phong"),
        ("/ServiceBookings/Index", "booking-dich-vu"),
        ("/RoomClasses/Index", "hang-phong"),

        // --- Vé máy bay ---
        ("/FlightTickets/Index", "ve-may-bay-doan"),
        ("/FlightTicketsIndividual/Index", "ve-may-bay-le"),

        // --- Hướng dẫn viên ---
        ("/GuideAssignments/Index", "dieu-hdv"),
        ("/GuideReport/Index", "bao-cao-hdv"),

        // --- Quản lý xe ---
        ("/Vehicles/Index", "kho-xe"),
        ("/VehiclePending/Index", "lich-xe-cho"),
        ("/VehicleAssignments/Index", "dieu-xe"),
        ("/VehicleReport/Index", "bao-cao-xe"),

        // --- Điều hành tour ---
        ("/ServiceOperations/Index", "phieu-dieu-hanh"),
        ("/OperationCalendar/Index", "lich-dieu-hanh"),

        // --- Tài chính ---
        ("/PendingReceipts/Index", "phieu-thu-cho"),
        ("/Receipts/Index", "phieu-thu"),
        ("/Payments/Index", "phieu-chi"),
        ("/Invoices/Index", "hoa-don"),
        ("/Invoices/Edit", "hoa-don/soan/{id:guid?}"),
        ("/CashFlowReport/Index", "dong-tien"),
        ("/CustomerDebtReport/Index", "cong-no-khach"),
        ("/ProviderDebtReport/Index", "cong-no-ncc"),

        // --- KPI ---
        ("/KpiConfig/Index", "thiet-lap-kpi"),

        // --- Hoa hồng ---
        ("/CommissionConfig/Index", "thiet-lap-hoa-hong"),
        ("/CommissionTiers/Index", "chinh-sach-hoa-hong"),
        ("/CustomerCommissionRules/Index", "hoa-hong-loai-khach"),
        ("/CommissionBySourceReport/Index", "hoa-hong-theo-nguon"),
        ("/CommissionByMilestoneReport/Index", "hoa-hong-theo-moc"),

        // --- Dự án & công việc ---
        ("/Workflows/Index", "du-an"),
        ("/WorkTasks/Index", "cong-viec"),
        ("/TaskPerformance/Index", "hieu-suat-cong-viec"),

        // --- Marketing ---
        ("/MarketingCampaigns/Index", "chien-dich"),
        ("/MessageTemplates/Index", "mau-tin-nhan"),
        ("/Posts/Index", "bai-viet"),
        ("/PostCategories/Index", "chuyen-muc"),

        // --- Báo cáo ---
        ("/SellerReport/Index", "bao-cao-nhan-vien"),
        ("/FinanceReport/Index", "bao-cao-tai-chinh"),
        ("/TourTypeReport/Index", "thu-chi-theo-tour"),
        ("/SystemReport/Index", "bao-cao-tong-hop"),

        // --- Đại lý B2B ---
        ("/Agents/Index", "dai-ly"),
        ("/AgentBookings/Index", "dat-cho-dai-ly"),

        // --- Hệ thống ---
        ("/Users/Index", "thanh-vien"),
        ("/Roles/Index", "vai-tro"),
        ("/ConfigHub/Index", "cau-hinh"),
        ("/Billing/Index", "goi-dich-vu"),
        ("/ActivityLogs/Index", "nhat-ky"),

        // --- Danh mục (nằm trong Cấu hình) ---
        ("/Branches/Index", "chi-nhanh"),
        ("/Departments/Index", "phong-ban"),
        ("/Positions/Index", "chuc-vu"),
        ("/CarTypes/Index", "loai-xe"),
        ("/Currencies/Index", "tien-te"),
        ("/CustomerSources/Index", "nguon-khach"),
        ("/CustomerTags/Index", "nhan-khach"),
        ("/CustomerTypes/Index", "loai-khach"),
        ("/LanguageTypes/Index", "ngon-ngu"),
        ("/MarketTypes/Index", "thi-truong"),
        ("/PaymentAccounts/Index", "tai-khoan-thanh-toan"),
        ("/Surcharges/Index", "phu-thu"),
        ("/TourGroups/Index", "nhom-tour"),
        ("/TransferReasons/Index", "ly-do-chuyen"),

        // --- Auth (trang người dùng cuối cũng gặp) ---
        ("/Auth/Login", "dang-nhap"),
        ("/Auth/Logout", "dang-xuat"),
        ("/Auth/Register", "dang-ky"),
        ("/Auth/ForgotPassword", "quen-mat-khau"),
        ("/Auth/ResetPassword", "dat-lai-mat-khau"),
    };

    /// <summary>
    /// Route PHỤ trỏ về cùng trang Index nhưng có đoạn "loai" để lọc — thay cho query string
    /// (/Orders?bookingType=0) vốn làm menu không sáng đúng và URL khó đọc. Không sinh 301.
    /// </summary>
    public static readonly IReadOnlyList<(string Page, string Route)> ExtraRoutes = new List<(string, string)>
    {
        ("/Orders/Index", "don-hang/loai/{loai}"),
        ("/Quotes/Index", "bao-gia/loai/{loai}"),
        ("/Providers/Index", "nha-cung-cap/loai/{loai}"),
        // Công việc: cùng trang danh sách, đoạn "pham-vi" lọc theo phạm vi (cua-toi = của người đăng nhập).
        ("/WorkTasks/Index", "cong-viec/{pham_vi}"),
        // Quản lý CHUYẾN theo loại sản phẩm (Tour FIT/GIT/LandTour) — bám staging /sample-tours,/group-tours,/single-tours.
        ("/Departures/Index", "chuyen-di/loai/{loai}"),
    };

    /// <summary>Slug loại nhà cung cấp → ProviderType. Guide 4 (HDV) · Airline 5 (vé máy bay).</summary>
    public static readonly IReadOnlyDictionary<string, int> ProviderLoai = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["hdv"] = 4, ["ve"] = 5, ["khach-san"] = 1, ["van-chuyen"] = 2, ["nha-hang"] = 3,
    };

    /// <summary>Slug loại đơn hàng (đoạn "loai" trong route) → BookingType. FIT 0·GIT 1·Landtour 2·Dịch vụ lẻ 4·Visa 5.</summary>
    public static readonly IReadOnlyDictionary<string, int> OrderLoai = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["tour-fit"] = 0, ["git"] = 1, ["landtour"] = 2, ["dich-vu-le"] = 4, ["visa"] = 5,
    };

    /// <summary>Slug loại báo giá → QuoteType. Tour 0·Combo 1·GIT 2·Landtour 3·Booking 4·Dịch vụ lẻ 5·Visa 6.</summary>
    public static readonly IReadOnlyDictionary<string, int> QuoteLoai = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["combo"] = 1, ["git"] = 2, ["landtour"] = 3, ["booking"] = 4, ["dich-vu-le"] = 5, ["visa"] = 6,
    };

    /// <summary>
    /// URL cũ (đường dẫn PascalCase, KHÔNG kèm phần {id}) → route tiếng Việt tương ứng, để 301.
    /// Chỉ map phần tĩnh; phần đuôi động (/{id}) middleware nối lại nguyên vẹn.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> LegacyToFriendly = BuildLegacyMap();

    private static Dictionary<string, string> BuildLegacyMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (page, route) in Pages)
        {
            // "/Orders/Index" → cũ "/Orders"; "/Orders/Detail" → cũ "/Orders/Detail".
            var oldPath = page.EndsWith("/Index", StringComparison.Ordinal)
                ? page[..^"/Index".Length]
                : page;

            // Route mới cắt bỏ phần {…} động để lấy tiền tố tĩnh.
            var brace = route.IndexOf('{', StringComparison.Ordinal);
            var newPath = "/" + (brace >= 0 ? route[..brace].TrimEnd('/') : route);

            // Bỏ qua trang gốc "" và tránh trùng khoá (Edit/Detail nhiều trang cùng tiền tố).
            if (oldPath.Length > 1 && !map.ContainsKey(oldPath))
            {
                map[oldPath] = newPath;
            }
        }
        return map;
    }
}
