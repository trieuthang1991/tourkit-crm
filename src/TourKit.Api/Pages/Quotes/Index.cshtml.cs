using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Quotes;

// Danh sách báo giá: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/quotes/QuotesPage.tsx): 6 KPI, thanh lọc (từ khoá + hạn hiệu lực + trạng thái
// + đã chuyển đơn), cột kép (mã/loại · khách/tour · số khách NL-TE-EB · giá trị + lợi nhuận),
// dòng tổng cộng trang, export CSV, hành động Sửa/Chuyển đơn/Mở đơn/Xoá.
[Authorize(Policy = "quote.view")]
public class IndexModel : TkListPageModel
{
    private readonly IQuoteService _svc;
    private readonly IQuoteConversionService _conversion;
    private readonly IDepartureService _departures;

    public IndexModel(IQuoteService svc, IQuoteConversionService conversion, IDepartureService departures)
    {
        _svc = svc;
        _conversion = conversion;
        _departures = departures;
    }

    public QuoteStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Text)> Departures { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "type")] public int? QuoteType { get; set; }

    public bool CanManage => User.HasClaim("perm", "quote.manage");

    /// <summary>
    /// Loại báo giá hiệu lực: ưu tiên đoạn "loai" trong route mới (/bao-gia/loai/combo) rồi mới tới
    /// query cũ (?type=1). Route giữ được khi DataTables gọi ?handler=Data trên cùng URL.
    /// </summary>
    private int? EffectiveType
    {
        get
        {
            if (RouteData.Values.TryGetValue("loai", out var raw) && raw is string slug
                && TourKit.Api.Routing.RouteMap.QuoteLoai.TryGetValue(slug, out var t))
            {
                return t;
            }
            return QuoteType;
        }
    }

    // Loại báo giá — bám QUOTE_TYPE_LABEL của bản cũ.
    public static readonly string[] TypeLabels =
        ["Tính giá Tour", "Tính giá Combo", "Tour GIT/Combo", "Landtour", "Booking Phòng", "Dịch vụ lẻ", "Visa"];

    public string TypeLabel => EffectiveType is int t && t >= 0 && t < TypeLabels.Length ? TypeLabels[t] : "Tất cả";

    public static string TypeName(int t) => t >= 0 && t < TypeLabels.Length ? TypeLabels[t] : "—";

    // Trạng thái báo giá: 0 nháp · 1 đã gửi · 2 chấp nhận · 3 từ chối.
    public static string StatusLabel(int s) => s switch
    {
        1 => "Đã gửi",
        2 => "Chấp nhận",
        3 => "Từ chối",
        _ => "Nháp",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "info",
        2 => "success",
        3 => "danger",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync(EffectiveType);
        Departures = (await _departures.ListAsync(1, TranDanhMuc.Chuyen))
            .Items.Select(d => (d.Id, Text: $"{d.Code} — {d.Title}")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí QuoteListFilter hỗ trợ.</summary>
    private QuoteListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;

        return new QuoteListFilter(
            Q: keyword,
            Status: I("status"),
            ValidFrom: D("validFrom"),
            ValidTo: D("validTo"),
            Converted: B("converted"),
            QuoteType: I("quoteType") ?? EffectiveType);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync(int.TryParse(Request.Query["quoteType"], out var qt) ? qt : EffectiveType);

        var items = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code,
            quoteType = x.QuoteType,
            typeLabel = TypeName(x.QuoteType),
            customerName = string.IsNullOrWhiteSpace(x.CustomerName) ? "—" : x.CustomerName,
            title = string.IsNullOrWhiteSpace(x.Title) ? "—" : x.Title,
            adults = x.Adults,
            children = x.Children,
            infants = x.Infants,
            totalAmount = x.TotalAmount,
            totalCost = x.TotalCost,
            totalProfit = x.TotalProfit,
            validUntil = x.ValidUntil?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
            convertedOrderId = x.ConvertedOrderId,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new
        {
            amount = items.Sum(x => x.totalAmount),
            cost = items.Sum(x => x.totalCost),
            profit = items.Sum(x => x.totalProfit),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>Thẻ thống kê dạng JSON — làm tươi KPI sau khi xoá/chuyển đơn mà không tải lại trang.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync(int.TryParse(Request.Query["quoteType"], out var qt) ? qt : EffectiveType);
        return new JsonResult(new
        {
            total = s.Total,
            totalAmount = s.TotalAmount,
            totalProfit = s.TotalProfit,
            accepted = s.Accepted,
            sent = s.Sent,
            rejected = s.Rejected,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã báo giá,Loại,Khách hàng,Tiêu đề,Người lớn,Trẻ em,Trẻ nhỏ,Tổng tiền,Lợi nhuận,Hạn hiệu lực,Trạng thái");
        foreach (var q in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(q.Code)).Append(',').Append(C(TypeName(q.QuoteType))).Append(',')
              .Append(C(q.CustomerName)).Append(',').Append(C(q.Title)).Append(',')
              .Append(q.Adults.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(q.Children.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(q.Infants.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(q.TotalAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(q.TotalProfit.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(q.ValidUntil?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(StatusLabel(q.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "bao-gia.csv");
    }

    /// <summary>Chuyển báo giá đã chấp nhận thành đơn (ghép chuyến sẵn có hoặc tạo chuyến FIT theo ngày).</summary>
    public async Task<IActionResult> OnPostConvertAsync(Guid id, Guid? tourDepartureId, DateTimeOffset? departureDate)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền chuyển báo giá thành đơn."));
        }

        if (tourDepartureId is null && departureDate is null)
        {
            return new JsonResult(Result.Error("Chọn chuyến khởi hành hoặc nhập ngày khởi hành (tour lẻ FIT)."));
        }

        try
        {
            var r = await _conversion.ConvertAsync(id, new ConvertQuoteDto(
                tourDepartureId,
                tourDepartureId is null ? TkDate.Day(departureDate) : null));
            return new JsonResult(Result.Success(
                $"Đã tạo đơn {r.OrderCode} (+{r.ServiceBookingCount.ToString(CultureInfo.InvariantCulture)} đặt dịch vụ).",
                new { orderId = r.OrderId }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá báo giá."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá báo giá."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
