using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.AgentBookings;

// Đặt chỗ đại lý (B2B): DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/agentBookings/AgentBookingsPage.tsx): 6 KPI, thanh lọc (mã booking + đại lý) +
// tab theo trạng thái, cột ghép (Booking·Đại lý · tổng tiền · trạng thái), dòng tổng cộng trang,
// export CSV, "Tạo từ báo giá" (CreateFromQuoteAsync) và quản lý hành khách (thêm/xoá).
[Authorize(Policy = "agentquote.view")]
public class IndexModel : TkListPageModel
{
    private readonly IAgentBookingService _svc;
    private readonly IAgentService _agents;
    private readonly IAgentQuoteRequestService _quotes;

    public IndexModel(IAgentBookingService svc, IAgentService agents, IAgentQuoteRequestService quotes)
    {
        _svc = svc;
        _agents = agents;
        _quotes = quotes;
    }

    public AgentBookingStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0m);
    public IReadOnlyList<(Guid Id, string Name)> Agents { get; private set; } = [];

    /// <summary>Chỉ yêu cầu báo giá đã Confirmed mới tạo được booking (điều kiện của service).</summary>
    public IReadOnlyList<(Guid Id, string Text)> ConfirmedQuotes { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "agentquote.manage");

    // Trạng thái đặt chỗ đại lý: 0 chờ · 1 đã xác nhận · 2 đã huỷ · 3 hoàn tất.
    public static readonly string[] StatusLabels = ["Chờ", "Đã xác nhận", "Đã huỷ", "Hoàn tất"];

    public static string StatusLabel(int s) => s >= 0 && s < StatusLabels.Length ? StatusLabels[s] : "—";

    public static string StatusColor(int s) => s switch
    {
        1 => "info",
        2 => "secondary",
        3 => "success",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Agents = (await _agents.ListAsync(1, TranDanhMuc.DaiLy)).Items.Select(a => (a.Id, a.Name)).ToList();
        ConfirmedQuotes = (await _quotes.ListAsync(1, 200, new AgentQuoteRequestListFilter(Status: (int)AgentQuoteStatus.Confirmed)))
            .Items.Select(q => (q.Id, Text: $"{q.ProductName} — {q.AgentName ?? "—"} ({(q.QuotedAmount ?? 0m).ToString("#,##0", CultureInfo.InvariantCulture)})")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí AgentBookingListFilter hỗ trợ.</summary>
    private AgentBookingListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

        return new AgentBookingListFilter(Q: keyword, AgentId: G("agentId"), Status: I("status"));
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
            agentId = x.AgentId,
            agentName = x.AgentName,
            quoteRequestId = x.QuoteRequestId,
            totalAmount = x.TotalAmount,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new { amount = items.Sum(x => x.totalAmount) };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>KPI dạng JSON — làm tươi sau khi tạo booking mà không tải lại trang.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync();
        return new JsonResult(new
        {
            total = s.Total,
            totalAmount = s.TotalAmount,
            pending = s.Pending,
            confirmed = s.Confirmed,
            done = s.Done,
            cancelled = s.Cancelled,
        });
    }

    /// <summary>Danh sách hành khách của 1 booking (modal "Hành khách" hệ cũ).</summary>
    public async Task<IActionResult> OnGetPassengersAsync(Guid id)
    {
        try
        {
            var b = await _svc.GetAsync(id);
            return new JsonResult(Result.Success(null, new
            {
                code = b.Code,
                passengers = b.Passengers.Select(p => new
                {
                    id = p.Id,
                    fullName = p.FullName,
                    dateOfBirth = p.DateOfBirth?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    passportNo = p.PassportNo,
                    nationality = p.Nationality,
                    note = p.Note,
                }).ToList(),
            }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã booking,Đại lý,Tổng tiền,Trạng thái");
        foreach (var b in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(b.Code)).Append(',').Append(C(b.AgentName)).Append(',')
              .Append(b.TotalAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(b.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "dat-cho-dai-ly.csv");
    }

    /// <summary>Tạo booking từ yêu cầu báo giá đã Confirmed.</summary>
    public async Task<IActionResult> OnPostCreateAsync(Guid quoteRequestId, string? code, string? note)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền tạo đặt chỗ đại lý."));
        }

        if (quoteRequestId == Guid.Empty)
        {
            return new JsonResult(Result.Error("Chọn yêu cầu báo giá đã xác nhận."));
        }

        try
        {
            var b = await _svc.CreateFromQuoteAsync(new CreateAgentBookingDto(quoteRequestId, code ?? "", note));
            return new JsonResult(Result.Success($"Đã tạo booking {b.Code}."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostAddPassengerAsync(
        Guid bookingId, string fullName, DateTimeOffset? dateOfBirth, string? passportNo, string? nationality, string? note)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền thêm hành khách."));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new JsonResult(Result.Error("Bắt buộc nhập họ tên hành khách."));
        }

        try
        {
            await _svc.AddPassengerAsync(bookingId, new AddAgentPassengerDto(
                fullName.Trim(), TkDate.Day(dateOfBirth), passportNo, nationality, note));
            return new JsonResult(Result.Success("Đã thêm hành khách."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostRemovePassengerAsync(Guid bookingId, Guid passengerId)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá hành khách."));
        }

        try
        {
            await _svc.RemovePassengerAsync(bookingId, passengerId);
            return new JsonResult(Result.Success("Đã xoá hành khách."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
