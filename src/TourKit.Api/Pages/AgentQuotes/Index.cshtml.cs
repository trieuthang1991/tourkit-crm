using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.AgentQuotes;

// Yêu cầu báo giá đại lý (B2B): DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/agentQuotes/AgentQuotesPage.tsx): 6 KPI, thanh lọc (sản phẩm/tour + đại lý) +
// tab theo trạng thái, cột ghép (đại lý/tour · số khách · ngày đi+về · giá chào+ghi chú · trạng thái),
// dòng tổng cộng trang, export CSV, hành động Gửi yêu cầu / Chào giá / Xác nhận / Từ chối.
[Authorize(Policy = "agentquote.view")]
public class IndexModel : TkListPageModel
{
    private readonly IAgentQuoteRequestService _svc;
    private readonly IAgentService _agents;

    public IndexModel(IAgentQuoteRequestService svc, IAgentService agents)
    {
        _svc = svc;
        _agents = agents;
    }

    public AgentQuoteStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0m);
    public IReadOnlyList<(Guid Id, string Name)> Agents { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "agentquote.manage");

    public static string StatusLabel(AgentQuoteStatus s) => s switch
    {
        AgentQuoteStatus.Requested => "Gửi yêu cầu",
        AgentQuoteStatus.Quoted => "Đã chào giá",
        AgentQuoteStatus.Confirmed => "Đã xác nhận",
        AgentQuoteStatus.Rejected => "Từ chối",
        _ => s.ToString(),
    };

    public static string StatusColor(AgentQuoteStatus s) => s switch
    {
        AgentQuoteStatus.Quoted => "info",
        AgentQuoteStatus.Confirmed => "success",
        AgentQuoteStatus.Rejected => "danger",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Agents = (await _agents.ListAsync(1, 500)).Items.Select(a => (a.Id, a.Name)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí AgentQuoteRequestListFilter hỗ trợ.</summary>
    private AgentQuoteRequestListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

        return new AgentQuoteRequestListFilter(Q: keyword, AgentId: G("agentId"), Status: I("status"));
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
            agentId = x.AgentId,
            agentName = x.AgentName,
            productName = x.ProductName,
            specialRequests = x.SpecialRequests,
            paxCount = x.PaxCount,
            travelDate = x.TravelDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            returnDate = x.ReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            quotedAmount = x.QuotedAmount,
            quotedNote = x.QuotedNote,
            status = (int)x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new
        {
            pax = items.Sum(x => x.paxCount),
            quoted = items.Sum(x => x.quotedAmount ?? 0m),
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

    /// <summary>KPI dạng JSON — làm tươi sau khi chào giá/xác nhận/từ chối mà không tải lại trang.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync();
        return new JsonResult(new
        {
            total = s.Total,
            requested = s.Requested,
            quoted = s.Quoted,
            confirmed = s.Confirmed,
            rejected = s.Rejected,
            totalQuoted = s.TotalQuoted,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Đại lý,Sản phẩm/Tour,Số khách,Ngày đi,Ngày về,Giá chào,Ghi chú chào giá,Yêu cầu riêng,Trạng thái");
        foreach (var q in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(q.AgentName)).Append(',').Append(C(q.ProductName)).Append(',')
              .Append(q.PaxCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(q.TravelDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(q.ReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append((q.QuotedAmount ?? 0m).ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(q.QuotedNote)).Append(',').Append(C(q.SpecialRequests)).Append(',')
              .Append(C(StatusLabel(q.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "bao-gia-dai-ly.csv");
    }

    /// <summary>Đại lý gửi yêu cầu báo giá (Requested).</summary>
    public async Task<IActionResult> OnPostCreateAsync(
        Guid agentId, string productName, DateTimeOffset? travelDate, DateTimeOffset? returnDate, int paxCount, string? specialRequests)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền gửi yêu cầu báo giá."));
        }

        try
        {
            await _svc.CreateAsync(new CreateAgentQuoteRequestDto(
                agentId, productName, travelDate?.ToUniversalTime(), returnDate?.ToUniversalTime(), paxCount, specialRequests));
            return new JsonResult(Result.Success("Đã gửi yêu cầu báo giá."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    /// <summary>Sales chào giá → Quoted.</summary>
    public async Task<IActionResult> OnPostQuoteAsync(Guid id, decimal quotedAmount, string? quotedNote)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền chào giá."));
        }

        try
        {
            await _svc.QuoteAsync(id, new QuoteAgentRequestDto(quotedAmount, quotedNote));
            return new JsonResult(Result.Success("Đã chào giá."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    /// <summary>Đại lý xác nhận báo giá → Confirmed (điều kiện tạo booking).</summary>
    public async Task<IActionResult> OnPostConfirmAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xác nhận báo giá."));
        }

        try
        {
            await _svc.ConfirmAsync(id);
            return new JsonResult(Result.Success("Đã xác nhận báo giá."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, string? note)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền từ chối báo giá."));
        }

        try
        {
            await _svc.RejectAsync(id, note);
            return new JsonResult(Result.Success("Đã từ chối báo giá."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
