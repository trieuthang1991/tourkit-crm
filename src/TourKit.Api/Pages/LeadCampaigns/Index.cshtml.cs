using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Pages.LeadCampaigns;

// Chia số Sale (chiến dịch chia lead): DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/leadCampaigns/LeadCampaignsPage.tsx): 4 thẻ KPI, lọc tên chiến dịch + người tạo,
// cột ghép (Chiến dịch = tên + người tạo · Số liệu = tổng lead + đã chăm sóc · Đơn chốt = số + tỷ lệ),
// thanh tiến độ, dòng tổng cộng trang, export CSV.
[Authorize(Policy = "lead.view")]
public class IndexModel : TkListPageModel
{
    private readonly ILeadCampaignService _svc;
    private readonly IUserAdminService _users;

    public IndexModel(ILeadCampaignService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

    public LeadCampaignStatsDto Stats { get; private set; } = new(0, 0, 0m, 0);
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public string? Note { get; set; }
    }

    public static string StatusLabel(int s) => s switch
    {
        1 => "Hoàn thành",
        0 => "Đang chạy",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "success",
        0 => "info",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng 2 tiêu chí LeadCampaignListFilter hỗ trợ.</summary>
    private LeadCampaignListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        return new LeadCampaignListFilter(Q: keyword, CreatedByUserId: G("createdByUserId"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + tổng cộng trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var items = result.Items.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            createdByName = c.CreatedByName,
            createdAt = c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            totalLeads = c.TotalLeads,
            caredCount = c.CaredCount,
            closedCount = c.ClosedCount,
            progress = c.Progress,
            closeRate = c.CloseRate,
            status = c.Status,
            statusLabel = StatusLabel(c.Status),
            statusColor = StatusColor(c.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new
        {
            totalLeads = items.Sum(x => x.totalLeads),
            caredCount = items.Sum(x => x.caredCount),
            closedCount = items.Sum(x => x.closedCount),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.TotalCampaigns,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Chiến dịch,Người tạo,Ngày tạo,Tổng lead,Đã chăm sóc,Tiến độ (%),Đơn chốt,Tỷ lệ chốt (%),Trạng thái");
        foreach (var c in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(c.Name)).Append(',').Append(C(c.CreatedByName)).Append(',')
              .Append(C(c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(c.TotalLeads.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(c.CaredCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(c.Progress.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(c.ClosedCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(c.CloseRate.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(c.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "chien-dich-chia-so.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        await _svc.CreateAsync(new CreateLeadCampaignDto(Input.Name, Input.Note));
        return new JsonResult(Result.Success("Đã lưu chiến dịch chia số."));
    }
}
