using TourKit.Api.Services;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.LeadCampaigns;

// Chia số Sale (chiến dịch chia lead): DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/leadCampaigns/LeadCampaignsPage.tsx): 4 thẻ KPI, lọc tên chiến dịch + người tạo,
// cột ghép (Chiến dịch = tên + người tạo · Số liệu = tổng lead + đã chăm sóc · Đơn chốt = số + tỷ lệ),
// thanh tiến độ, dòng tổng cộng trang, export CSV.
[Authorize(Policy = "lead.view")]
public class IndexModel : TkListPageModel
{
    private readonly ILeadCampaignService _svc;
    private readonly UserDirectory _users;

    public IndexModel(ILeadCampaignService svc, UserDirectory users)
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

        /// <summary>Cách chia số — xem <see cref="LeadAssignMode"/>.</summary>
        public int AssignMode { get; set; }

        /// <summary>Nhóm nhân viên nhận số. THỨ TỰ ở đây chính là thứ tự vòng chia.</summary>
        public List<Guid> Assignees { get; set; } = [];
    }

    public static string AssignModeLabel(int m) => LeadAssignModeText.Vi((LeadAssignMode)m);

    public static string AssignModeColor(int m) => m switch
    {
        (int)LeadAssignMode.XoayVong => "primary",
        (int)LeadAssignMode.NgauNhien => "info",
        _ => "secondary",
    };

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

        // Tên nhân viên đọc qua danh bạ có CACHE, không tra thẳng bảng Users mỗi lần lật trang.
        var userNames = await _users.NamesAsync();

        var items = result.Items.Select(c => new
        {
            id = c.Id,
            code = c.Code,
            name = c.Name,
            assignMode = c.AssignMode,
            assignModeLabel = AssignModeLabel(c.AssignMode),
            assignModeColor = AssignModeColor(c.AssignMode),
            assignees = c.Assignees ?? [],
            assigneeNames = (c.Assignees ?? [])
                .Select(u => userNames.GetValueOrDefault(u))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList(),
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

        // Sửa được chứ không chỉ tạo mới: nhóm chia số là thứ thay đổi thường xuyên (có người nghỉ,
        // có người mới vào), mà trước đây màn này không có đường sửa nào.
        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateLeadCampaignDto(
                Input.Name, Input.Note, Input.AssignMode, Input.Assignees));
            return new JsonResult(Result.Success("Đã lưu chiến dịch chia số."));
        }

        var moi = await _svc.CreateAsync(new CreateLeadCampaignDto(
            Input.Name, Input.Note, Input.AssignMode, Input.Assignees));

        // Nói luôn cái mã vừa sinh: đó là thứ người dựng form thu lead cần cầm đi, không nói ra thì
        // họ phải mò tìm trong lưới.
        return new JsonResult(Result.Success($"Đã tạo chiến dịch {moi.Code}."));
    }

    /// <summary>Đổi nhanh trạng thái Đang chạy/Hoàn thành từ menu trên dòng lưới.</summary>
    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, int status)
    {
        try
        {
            await _svc.SetStatusAsync(id, status);
            return new JsonResult(Result.Success(status == 1 ? "Đã đánh dấu hoàn thành chiến dịch." : "Đã chuyển chiến dịch về đang chạy."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
