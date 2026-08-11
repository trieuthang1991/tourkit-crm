using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Catalog;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;
using TourKit.Application.Providers;

namespace TourKit.Api.Pages.FlightTickets;

// Vé máy bay đoàn: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/flights/FlightTicketsPage.tsx): 7 KPI, thanh lọc (PNR + thị trường + NCC + loại hình
// + tab Tất cả/Đã gán/Chưa gán), cột kép (PNR/loại hình · gán tour · NCC/thị trường · lịch trình ·
// hành trình chặng bay · SL/dùng/còn · tổng chi/đã TT · còn lại/bảo lưu), dòng tổng cộng trang, export CSV,
// hành động Thêm/Sửa/Gán tour/Xoá.
[Authorize(Policy = "ticketfund.view")]
public class IndexModel : TkListPageModel
{
    private readonly IFlightTicketService _svc;
    private readonly IMarketTypeService _markets;
    private readonly IProviderService _providers;
    private readonly IBookingService _orders;

    public IndexModel(IFlightTicketService svc, IMarketTypeService markets, IProviderService providers, IBookingService orders)
    {
        _svc = svc;
        _markets = markets;
        _providers = providers;
        _orders = orders;
    }

    public FlightTicketStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(string Id, string Name)> Markets { get; private set; } = [];
    public IReadOnlyList<(string Id, string Name)> Providers { get; private set; } = [];
    public IReadOnlyList<(string Id, string Text)> Orders { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "ticketfund.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập PNR")] public string Pnr { get; set; } = "";
        public string? TourType { get; set; }
        public int Days { get; set; }
        public DateTimeOffset? DepartureDate { get; set; }
        public int Quantity { get; set; }
        public int UsedQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReservedAmount { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }

        // Thị trường/NCC: có lookup nhưng ref có thể là id legacy → select giữ luôn giá trị cũ (xem fill() ở view).
        public string? MarketRef { get; set; }
        public string? ProviderRef { get; set; }

        // Passthrough (không dựng editor): giữ nguyên khi sửa để KHÔNG mất dữ liệu gán tour / chặng bay.
        public string? OrderRef { get; set; }
        public string? SegmentsJson { get; set; }
    }

    /// <summary>Loại hình vé đoàn — bám FLIGHT_TOUR_TYPE hệ cũ.</summary>
    public static readonly (string Value, string Label)[] TourTypes =
        [("inbound", "Inbound"), ("outbound", "Outbound"), ("domestic", "Nội địa")];

    public static string TourTypeLabel(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
        {
            return "—";
        }

        foreach (var t in TourTypes)
        {
            if (string.Equals(t.Value, v, StringComparison.OrdinalIgnoreCase))
            {
                return t.Label;
            }
        }

        return v;
    }

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Markets = (await _markets.ListAsync()).Select(m => (m.Id.ToString(), m.Name)).ToList();
        // Ô chọn NCC nay gọi server (/api/v1/lookup/providers) nên không nạp danh mục xuống trang nữa.
        Orders = (await _orders.ListOrdersAsync(1, TranDanhMuc.DonHang)).Items
            .Select(o => (o.Id.ToString(), Text: $"{o.Code} — {o.CustomerName ?? "—"}")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí FlightTicketListFilter hỗ trợ.</summary>
    private FlightTicketListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;

        return new FlightTicketListFilter(
            Q: keyword,
            MarketRef: S("marketRef"),
            ProviderRef: S("providerRef"),
            TourType: S("tourType"),
            Days: I("days"),
            DepartureFrom: D("departureFrom"),
            Assigned: B("assigned"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var filter = BuildFilter(dt.Keyword);
        var result = await _svc.ListAsync(dt.Page, dt.Size, filter);
        var total = (await _svc.GetStatsAsync()).Total;

        var items = result.Items.Select(x => new
        {
            id = x.Id,
            pnr = x.Pnr,
            tourType = x.TourType,
            tourTypeLabel = TourTypeLabel(x.TourType),
            marketRef = x.MarketRef,
            marketName = x.MarketName,
            providerRef = x.ProviderRef,
            providerName = x.ProviderName,
            orderRef = x.OrderRef,
            orderCode = x.OrderCode,
            orderName = x.OrderName,
            days = x.Days,
            departureDate = x.DepartureDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            departureDateText = x.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            segments = x.Segments.Select(s => new { date = s.Date ?? "", flightNo = s.FlightNo ?? "", from = s.From ?? "", to = s.To ?? "", depTime = s.DepTime ?? "" }).ToList(),
            // Passthrough hành trình: PHẢI đúng khuôn { "segments": [...] } để FlightItinerary.Parse đọc lại được.
            segmentsJson = new FlightItinerary { Segments = x.Segments }.ToJsonOrNull() ?? "",
            quantity = x.Quantity,
            usedQuantity = x.UsedQuantity,
            remainingQuantity = x.RemainingQuantity,
            totalCost = x.TotalCost,
            paidAmount = x.PaidAmount,
            remainingCost = x.RemainingCost,
            reservedAmount = x.ReservedAmount,
            status = x.Status,
            note = x.Note,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new
        {
            quantity = items.Sum(x => x.quantity),
            used = items.Sum(x => x.usedQuantity),
            remaining = items.Sum(x => x.remainingQuantity),
            cost = items.Sum(x => x.totalCost),
            paid = items.Sum(x => x.paidAmount),
            remainingCost = items.Sum(x => x.remainingCost),
            reserved = items.Sum(x => x.reservedAmount),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>KPI theo bộ lọc đang áp (làm tươi sau khi lọc/lưu/xoá mà không tải lại trang).</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync(BuildFilter(Request.Query["search"].ToString() is { Length: > 0 } k ? k : null));
        return new JsonResult(new
        {
            total = s.Total,
            assigned = s.Assigned,
            unassigned = s.Unassigned,
            totalQuantity = s.TotalQuantity,
            totalUsed = s.TotalUsed,
            totalRemaining = s.TotalRemaining,
            totalCost = s.TotalCost,
            totalPaid = s.TotalPaid,
            totalRemainingCost = s.TotalRemainingCost,
            totalReserved = s.TotalReserved,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("PNR,Loại hình,Thị trường,Nhà cung cấp,Đơn gán,Ngày đi,Số ngày,Hành trình,Số lượng,Đã dùng,Còn lại,Tổng chi,Đã TT,Còn lại tiền,Bảo lưu");
        foreach (var t in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            var route = string.Join(" · ", t.Segments.Select(s => $"{s.Date} {s.FlightNo} {s.From}-{s.To} {s.DepTime}".Trim()));
            sb.Append(C(t.Pnr)).Append(',').Append(C(TourTypeLabel(t.TourType))).Append(',')
              .Append(C(t.MarketName)).Append(',').Append(C(t.ProviderName)).Append(',')
              .Append(C(t.OrderCode)).Append(',')
              .Append(C(t.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(t.Days.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(route)).Append(',')
              .Append(t.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.UsedQuantity.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.RemainingQuantity.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.TotalCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.PaidAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.RemainingCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.ReservedAmount.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "ve-may-bay-doan.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa vé máy bay đoàn."));
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        var segments = FlightItinerary.Parse(Input.SegmentsJson).Segments;

        // Select rỗng gửi lên chuỗi "" → chuẩn hoá về null để không lưu ref rác.
        static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateFlightTicketDto(
                    Input.Pnr, N(Input.MarketRef), N(Input.ProviderRef), N(Input.TourType), Input.Days, TkDate.Day(Input.DepartureDate),
                    Input.Quantity, Input.UsedQuantity, N(Input.OrderRef), Input.TotalCost, Input.PaidAmount, Input.ReservedAmount,
                    Input.Status, Input.Note, segments));
            }
            else
            {
                await _svc.CreateAsync(new CreateFlightTicketDto(
                    Input.Pnr, N(Input.MarketRef), N(Input.ProviderRef), N(Input.TourType), Input.Days, TkDate.Day(Input.DepartureDate),
                    Input.Quantity, Input.TotalCost, Input.ReservedAmount, Input.Note, segments));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu vé máy bay đoàn."));
    }

    /// <summary>Gán vé đoàn vào 1 đơn (orderRef rỗng = gỡ gán) — bám nút "+ Gán tour" hệ cũ.</summary>
    public async Task<IActionResult> OnPostAssignAsync(Guid id, string? orderRef)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền gán tour."));
        }

        try
        {
            await _svc.AssignAsync(id, new AssignFlightTicketDto(string.IsNullOrWhiteSpace(orderRef) ? null : orderRef));
            return new JsonResult(Result.Success("Đã gán tour cho vé đoàn."));
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
            return new JsonResult(Result.Error("Bạn không có quyền xoá vé máy bay đoàn."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá vé máy bay đoàn."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
