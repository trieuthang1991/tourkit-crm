using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Providers;

namespace TourKit.Api.Pages.TicketFunds;

// Quỹ vé ứng (legacy TicketFund) — KHÁC màn FlightTickets (vé máy bay đoàn theo PNR): quỹ vé gắn theo ĐƠN + NCC.
// DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ (web/src/features/ticketFunds/TicketFundsPage.tsx):
// 3 KPI, thanh lọc (mã vé + NCC) + tab Tất cả/Chưa đóng/Đã đóng, cột (mã đơn · mã vé · NCC · trạng thái ·
// đóng quỹ), modal thêm/sửa + xoá, export CSV.
[Authorize(Policy = "ticketfund.view")]
public class IndexModel : TkListPageModel
{
    private readonly ITicketFundService _svc;
    private readonly IProviderService _providers;
    private readonly IBookingService _orders;
    private readonly IProviderServiceService _providerServices;

    public IndexModel(
        ITicketFundService svc,
        IProviderService providers,
        IBookingService orders,
        IProviderServiceService providerServices)
    {
        _svc = svc;
        _providers = providers;
        _orders = orders;
        _providerServices = providerServices;
    }

    public TicketFundStatsDto Stats { get; private set; } = new(0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Providers { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "ticketfund.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã đơn (orderId)")] public Guid OrderId { get; set; }
        public Guid? ProviderId { get; set; }
        public Guid? ProviderServiceId { get; set; }
        public string? TicketCode { get; set; }
        public int Status { get; set; }
        public bool IsClosed { get; set; }
    }

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Providers = (await _providers.ListAsync(1, 500)).Items.Select(p => (p.Id, p.Name)).ToList();
    }

    /// <summary>
    /// Tìm đơn hàng cho ô chọn (select2 gọi server, gõ tới đâu lọc tới đó).
    ///
    /// Trước đây ô này là input text ghi "Dán ID đơn hàng (GUID)" — không ai gõ hay nhớ nổi một GUID.
    /// Đổ hết đơn vào một dropdown cũng không được: hệ thống có hàng nghìn đơn. Nên phải là tìm kiếm
    /// phía server, mỗi lượt chỉ trả 20 dòng khớp nhất.
    /// </summary>
    public async Task<IActionResult> OnGetOrderSearchAsync(string? q)
    {
        var result = await _orders.ListOrdersAsync(1, 20, new OrderListFilter(Q: q));
        return new JsonResult(new
        {
            results = result.Items.Select(o => new
            {
                id = o.Id,
                text = $"{o.Code} — {o.CustomerName ?? "chưa có khách"}",
            }),
        });
    }

    /// <summary>
    /// Tìm giá dịch vụ, LỌC THEO nhà cung cấp đang chọn. Không lọc thì người dùng thấy giá của mọi
    /// NCC và rất dễ gắn nhầm giá của nhà cung cấp khác vào quỹ vé.
    /// </summary>
    public async Task<IActionResult> OnGetServiceSearchAsync(Guid? providerId, string? q)
    {
        if (providerId is null)
        {
            return new JsonResult(new { results = Array.Empty<object>() });
        }

        var result = await _providerServices.ListAsync(1, 50, providerId);
        var keyword = q?.Trim();

        var items = result.Items
            .Where(s => string.IsNullOrEmpty(keyword)
                || (s.PriceName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(s => new
            {
                id = s.Id,
                text = $"{s.PriceName ?? "Không tên"} — {s.ContractPrice.ToString("#,##0", CultureInfo.InvariantCulture)}",
            });

        return new JsonResult(new { results = items });
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí TicketFundListFilter hỗ trợ.</summary>
    private TicketFundListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;

        return new TicketFundListFilter(
            Q: keyword,
            ProviderId: G("providerId"),
            OrderId: G("orderId"),
            Status: I("status"),
            IsClosed: B("isClosed"));
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
            orderId = x.OrderId,
            orderCode = x.OrderCode,
            providerId = x.ProviderId,
            providerName = x.ProviderName,
            providerServiceId = x.ProviderServiceId,
            ticketCode = x.TicketCode,
            status = x.Status,
            isClosed = x.IsClosed,
        }).ToList();

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = total,
            recordsFiltered = result.Total,
            data = items,
        });
    }

    /// <summary>KPI dạng JSON — làm tươi sau khi thêm/sửa/xoá mà không tải lại trang.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync();
        return new JsonResult(new { total = s.Total, closed = s.Closed, open = s.Open });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã vé,Mã đơn,Nhà cung cấp,Trạng thái,Đóng quỹ");
        foreach (var t in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(t.TicketCode)).Append(',').Append(C(t.OrderCode)).Append(',').Append(C(t.ProviderName)).Append(',')
              .Append(t.Status.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(t.IsClosed ? "Đã đóng" : "Đang mở")).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "quy-ve-ung.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa quỹ vé ứng."));
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateTicketFundDto(
                    Input.ProviderId, Input.ProviderServiceId, Input.TicketCode, Input.Status, Input.IsClosed));
            }
            else
            {
                await _svc.CreateAsync(new CreateTicketFundDto(
                    Input.OrderId, Input.ProviderId, Input.ProviderServiceId, Input.TicketCode, Input.Status, Input.IsClosed));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu quỹ vé ứng."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá quỹ vé ứng."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá quỹ vé ứng."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
