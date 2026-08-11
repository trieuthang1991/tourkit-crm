using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Flights;
using TourKit.Application.Flights.Dtos;
using TourKit.Application.Providers;

namespace TourKit.Api.Pages.FlightTicketsIndividual;

// Vé máy bay lẻ: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/flights/FlightTicketsIndividualPage.tsx): 7 KPI P/L, thanh lọc (từ khoá + NCC +
// trạng thái duyệt) + 10 sub-tab, cột kép (vé/khách · NCC/đơn · hành trình · thu/còn nợ · chi/phải chi ·
// lợi nhuận · trạng thái+hạn chi), dòng tổng cộng trang, export CSV, offcanvas thêm/sửa + xoá.
[Authorize(Policy = "ticketfund.view")]
public class IndexModel : TkListPageModel
{
    private readonly IFlightTicketIndividualService _svc;
    private readonly IProviderService _providers;

    public IndexModel(IFlightTicketIndividualService svc, IProviderService providers)
    {
        _svc = svc;
        _providers = providers;
    }

    public FlightTicketIndividualStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(string Id, string Name)> Providers { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "ticketfund.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã vé")] public string Code { get; set; } = "";
        public string? TicketCode { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập PNR")] public string Pnr { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên khách")] public string CustomerName { get; set; } = "";
        public int TripType { get; set; }
        public string? Route { get; set; }
        public DateTimeOffset? DepartDate { get; set; }
        public DateTimeOffset? ReturnDate { get; set; }
        public decimal SellAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTimeOffset? PaymentDueDate { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }

        // NCC có lookup (ref có thể là id legacy → select giữ luôn giá trị cũ, xem fill() ở view).
        public string? ProviderRef { get; set; }

        // Passthrough (không có lookup): giữ nguyên khi sửa để không xoá dữ liệu.
        public string? OrderRef { get; set; }
        public string? AssigneeRef { get; set; }
    }

    /// <summary>Sub-tab bám hệ cũ (FI_TABS) — value khớp tham số Tab của service.</summary>
    public static readonly (string Value, string Label, string StatKey)[] Tabs =
    [
        ("new", "Tạo mới", "new"),
        ("approved", "Đã duyệt", "approved"),
        ("rejected", "Không duyệt", "rejected"),
        ("pending-pay", "Chờ chi", "pendingPay"),
        ("due-soon", "Đến hạn chi 24h", "dueSoon"),
        ("overdue", "Quá hạn chi", "overdue"),
        ("partial-pay", "Chưa chi hết", "partialPay"),
        ("success", "Thành công", "success"),
        ("partial-receive", "Chưa thu hết", "partialReceive"),
    ];

    public int TabCount(string statKey) => statKey switch
    {
        "new" => Stats.New,
        "approved" => Stats.Approved,
        "rejected" => Stats.Rejected,
        "pendingPay" => Stats.PendingPay,
        "dueSoon" => Stats.DueSoon,
        "overdue" => Stats.Overdue,
        "partialPay" => Stats.PartialPay,
        "success" => Stats.Success,
        "partialReceive" => Stats.PartialReceive,
        _ => Stats.Total,
    };

    public static string TripTypeLabel(int t) => t switch { 1 => "Khứ hồi", _ => "Một chiều" };

    public static string StatusLabel(int s) => s switch { 1 => "Đã duyệt", 2 => "Không duyệt", _ => "Tạo mới" };

    public static string StatusColor(int s) => s switch { 1 => "success", 2 => "danger", _ => "info" };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        // Ô chọn NCC nay gọi server (?handler=ProviderLookup) nên không nạp danh mục xuống trang nữa.
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí FlightTicketIndividualListFilter hỗ trợ.</summary>
    private FlightTicketIndividualListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new FlightTicketIndividualListFilter(
            Q: keyword,
            ProviderRef: S("providerRef"),
            Status: I("status"),
            Tab: S("tab"),
            DepartFrom: D("departFrom"),
            DepartTo: D("departTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var total = (await _svc.GetStatsAsync()).Total;

        var items = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code,
            ticketCode = x.TicketCode,
            pnr = x.Pnr,
            customerName = x.CustomerName,
            orderRef = x.OrderRef,
            orderCode = x.OrderCode,
            providerRef = x.ProviderRef,
            providerName = x.ProviderName,
            assigneeRef = x.AssigneeRef,
            tripType = x.TripType,
            tripTypeLabel = TripTypeLabel(x.TripType),
            route = x.Route,
            departDate = x.DepartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            departDateText = x.DepartDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            returnDate = x.ReturnDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            returnDateText = x.ReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            sellAmount = x.SellAmount,
            receivedAmount = x.ReceivedAmount,
            receivableRemaining = x.ReceivableRemaining,
            totalCost = x.TotalCost,
            paidAmount = x.PaidAmount,
            payableRemaining = x.PayableRemaining,
            profit = x.Profit,
            paymentDueDate = x.PaymentDueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            paymentDueDateText = x.PaymentDueDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
            note = x.Note,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI (bám footer P/L hệ cũ).
        var pageSum = new
        {
            sell = items.Sum(x => x.sellAmount),
            received = items.Sum(x => x.receivedAmount),
            receivable = items.Sum(x => x.receivableRemaining),
            cost = items.Sum(x => x.totalCost),
            paid = items.Sum(x => x.paidAmount),
            payable = items.Sum(x => x.payableRemaining),
            profit = items.Sum(x => x.profit),
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

    /// <summary>KPI + số đếm sub-tab theo bộ lọc đang áp.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } k ? k : null;
        // Số đếm sub-tab phải độc lập với tab đang chọn → bỏ tiêu chí Tab khi tính thẻ.
        var s = await _svc.GetStatsAsync(BuildFilter(keyword) with { Tab = null });
        return new JsonResult(new
        {
            total = s.Total,
            @new = s.New,
            approved = s.Approved,
            rejected = s.Rejected,
            pendingPay = s.PendingPay,
            dueSoon = s.DueSoon,
            overdue = s.Overdue,
            partialPay = s.PartialPay,
            success = s.Success,
            partialReceive = s.PartialReceive,
            totalSell = s.TotalSell,
            totalReceived = s.TotalReceived,
            totalReceivable = s.TotalReceivable,
            totalCost = s.TotalCost,
            totalPaid = s.TotalPaid,
            totalPayable = s.TotalPayable,
            totalProfit = s.TotalProfit,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã vé,Số vé,PNR,Khách,NCC,Đơn hàng,Loại vé,Hành trình,Ngày đi,Ngày về,Tổng thu,Thực thu,Còn nợ thu,Tổng chi,Thực chi,Phải chi,Lợi nhuận,Hạn chi,Trạng thái");
        foreach (var t in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(t.Code)).Append(',').Append(C(t.TicketCode)).Append(',').Append(C(t.Pnr)).Append(',')
              .Append(C(t.CustomerName)).Append(',').Append(C(t.ProviderName)).Append(',').Append(C(t.OrderCode)).Append(',')
              .Append(C(TripTypeLabel(t.TripType))).Append(',').Append(C(t.Route)).Append(',')
              .Append(C(t.DepartDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(t.ReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(t.SellAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.ReceivedAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.ReceivableRemaining.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.TotalCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.PaidAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.PayableRemaining.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(t.Profit.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(t.PaymentDueDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(StatusLabel(t.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "ve-may-bay-le.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa vé máy bay lẻ."));
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        // Select rỗng gửi lên chuỗi "" → chuẩn hoá về null để không lưu ref rác.
        static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        Input.OrderRef = N(Input.OrderRef);
        Input.ProviderRef = N(Input.ProviderRef);
        Input.AssigneeRef = N(Input.AssigneeRef);

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateFlightTicketIndividualDto(
                    Input.Code, Input.TicketCode, Input.Pnr, Input.CustomerName, Input.OrderRef, Input.ProviderRef,
                    Input.TripType, Input.Route, TkDate.Day(Input.DepartDate), TkDate.Day(Input.ReturnDate),
                    Input.SellAmount, Input.ReceivedAmount, Input.TotalCost, Input.PaidAmount,
                    TkDate.Day(Input.PaymentDueDate), Input.Status, Input.AssigneeRef, Input.Note));
            }
            else
            {
                await _svc.CreateAsync(new CreateFlightTicketIndividualDto(
                    Input.Code, Input.TicketCode, Input.Pnr, Input.CustomerName, Input.OrderRef, Input.ProviderRef,
                    Input.TripType, Input.Route, TkDate.Day(Input.DepartDate), TkDate.Day(Input.ReturnDate),
                    Input.SellAmount, Input.ReceivedAmount, Input.TotalCost, Input.PaidAmount,
                    TkDate.Day(Input.PaymentDueDate), Input.Status, Input.AssigneeRef, Input.Note));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu vé máy bay lẻ."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá vé máy bay lẻ."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá vé máy bay lẻ."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
