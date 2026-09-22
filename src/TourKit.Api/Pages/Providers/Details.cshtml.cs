using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Catalog;
using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Pages.Providers;

// Hồ sơ 360 của MỘT nhà cung cấp — gom hết những gì đang rải rác ở form sửa NCC, /bang-gia-ncc,
// /quy-ve và đơn hàng vào một trang chỉ-đọc. Không sửa gì ở đây: nút "Sửa" đưa về danh sách
// (/nha-cung-cap) nơi có offcanvas sửa. Mọi truy vấn đều CÓ BIÊN (page/size), không get-all.
[Authorize(Policy = "provider.view")]
public class DetailsModel : PageModel
{
    private readonly IProviderService _svc;
    private readonly IProviderServiceService _providerServices;
    private readonly ITicketFundService _ticketFunds;
    private readonly IBranchService _branches;
    private readonly TourKit.Api.Services.MarketDirectory _marketTypes;

    public DetailsModel(
        IProviderService svc,
        IProviderServiceService providerServices,
        ITicketFundService ticketFunds,
        IBranchService branches,
        TourKit.Api.Services.MarketDirectory marketTypes)
    {
        _svc = svc;
        _providerServices = providerServices;
        _ticketFunds = ticketFunds;
        _branches = branches;
        _marketTypes = marketTypes;
    }

    /// <summary>Trần dòng nạp cho bảng giá / quỹ vé — bằng MaxPageSize, đủ cho một hồ sơ NCC mà không get-all.</summary>
    private const int MaxLines = 200;

    public ProviderDto Provider { get; private set; } = default!;
    public ProviderProfile Profile => Provider.Profile ?? new ProviderProfile();

    /// <summary>Bảng giá và dịch vụ của NCC này (nguồn /bang-gia-ncc).</summary>
    public IReadOnlyList<ProviderServiceDto> PriceLines { get; private set; } = [];

    /// <summary>Quỹ vé ứng gắn theo NCC này (nguồn /quy-ve).</summary>
    public IReadOnlyList<TicketFundDto> TicketFunds { get; private set; } = [];

    /// <summary>Tên thị trường / chi nhánh — NCC chỉ giữ Id, phải tra danh mục (nhỏ) để hiện tên.</summary>
    public string? MarketTypeName { get; private set; }
    public string? BranchName { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // GetAsync ném NotFoundException khi không có — trang chi tiết bắt lại rồi đưa về danh sách,
        // dễ hiểu hơn một màn lỗi 404 trần cho người dùng cuối.
        try
        {
            Provider = await _svc.GetAsync(id);
        }
        catch (NotFoundException)
        {
            return Redirect("/nha-cung-cap");
        }

        // Công nợ (tổng mua / đã trả / còn nợ) CHỈ được ListAsync bơm vào theo trang; GetAsync trả 0.
        // Lấy đúng dòng của NCC này bằng một truy vấn danh sách lọc theo MÃ (Code là duy nhất) rồi
        // khớp đúng Id — không nạp cả bảng, không tự cộng lại công nợ ở tầng trang.
        var enriched = await _svc.ListAsync(1, MaxLines, new ProviderListFilter(Q: Provider.Code));
        if (enriched.Items.FirstOrDefault(x => x.Id == id) is { } withDebt)
        {
            Provider = withDebt;
        }

        // Bảng giá & quỹ vé — phân trang tại server, chỉ lấy đúng NCC này.
        PriceLines = (await _providerServices.ListAsync(1, MaxLines, id)).Items;
        TicketFunds = (await _ticketFunds.ListAsync(1, MaxLines, new TicketFundListFilter(ProviderId: id))).Items;

        // Danh mục nhỏ → nạp để đổi Id thành tên hiển thị.
        var markets = await _marketTypes.ListAsync();
        var branches = await _branches.ListAsync();
        MarketTypeName = markets.FirstOrDefault(m => m.Id == Provider.MarketTypeId)?.Name;
        BranchName = branches.FirstOrDefault(b => b.Id == Provider.BranchId)?.Name;

        return Page();
    }

    /// <summary>Tiền theo định dạng vi-VN (dấu chấm ngăn nghìn) — dùng chung cho KPI công nợ và bảng giá.</summary>
    public static string Tien(decimal v) => v.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN"));

    /// <summary>2 chữ cái đầu cho avatar (khi không có ảnh) — bám DetailsModel của Khách hàng.</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return "?"; }
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
