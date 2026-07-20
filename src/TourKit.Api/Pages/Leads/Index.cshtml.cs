using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Catalog;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Leads;

// Cơ hội bán hàng (Lead): DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/leads/LeadsPage.tsx): 7 thẻ KPI, 7 tiêu chí lọc đẩy xuống SQL,
// cột ghép (Cơ hội = tên + chi nhánh · Liên hệ = SĐT + email), nút Chuyển thành KH, export CSV.
[Authorize(Policy = "lead.view")]
public class IndexModel : TkListPageModel
{
    private readonly ILeadService _svc;
    private readonly IUserAdminService _users;
    private readonly IBranchService _branches;

    public IndexModel(ILeadService svc, IUserAdminService users, IBranchService branches)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
    }

    public LeadStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<string> Sources { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Branches { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập họ tên")] public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public Guid? AssignedToUserId { get; set; }
        public Guid? BranchId { get; set; }
    }

    public static string StatusLabel(LeadStatus s) => s switch
    {
        LeadStatus.New => "Mới",
        LeadStatus.Contacted => "Đã liên hệ",
        LeadStatus.Qualified => "Tiềm năng",
        LeadStatus.Won => "Đã chốt",
        LeadStatus.Lost => "Thất bại",
        _ => "—",
    };

    public static string StatusColor(LeadStatus s) => s switch
    {
        LeadStatus.Won => "success",
        LeadStatus.Lost => "danger",
        LeadStatus.Qualified => "info",
        LeadStatus.Contacted => "warning",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Sources = (await _svc.GetFilterOptionsAsync()).Sources;
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        Branches = (await _branches.ListAsync()).Select(b => (b.Id, b.Name)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng 7 tiêu chí LeadListFilter hỗ trợ.</summary>
    private LeadListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();

        return new LeadListFilter(
            Q: keyword,
            Status: I("status"),
            Source: S("source"),
            AssignedToUserId: G("assignedToUserId"),
            CreatedFrom: D("createdFrom"),
            CreatedTo: D("createdTo"),
            BranchId: G("branchId"),
            CreatedByUserId: G("createdByUserId"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var userNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var branchNames = (await _branches.ListAsync()).ToDictionary(b => b.Id, b => b.Name);

        var items = result.Items.Select(l => new
        {
            id = l.Id,
            fullName = l.FullName,
            phone = l.Phone,
            email = l.Email,
            source = l.Source,
            status = (int)l.Status,
            statusLabel = StatusLabel(l.Status),
            statusColor = StatusColor(l.Status),
            assignedToUserId = l.AssignedToUserId,
            assigneeName = l.AssignedToUserId is Guid u && userNames.TryGetValue(u, out var un) ? un : null,
            branchId = l.BranchId,
            branchName = l.BranchId is Guid b && branchNames.TryGetValue(b, out var bn) ? bn : null,
            convertedCustomerId = l.ConvertedCustomerId,
        }).ToList();

        return DtJson(dt.Draw, stats.Total, result.Total, items);
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));
        var userNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var branchNames = (await _branches.ListAsync()).ToDictionary(b => b.Id, b => b.Name);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Họ tên,SĐT,Email,Nguồn,Phụ trách,Chi nhánh,Trạng thái,Đã chuyển KH");
        foreach (var l in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            var assignee = l.AssignedToUserId is Guid u && userNames.TryGetValue(u, out var un) ? un : "";
            var branch = l.BranchId is Guid b && branchNames.TryGetValue(b, out var bn) ? bn : "";
            sb.Append(C(l.FullName)).Append(',').Append(C(l.Phone)).Append(',').Append(C(l.Email)).Append(',')
              .Append(C(l.Source)).Append(',').Append(C(assignee)).Append(',').Append(C(branch)).Append(',')
              .Append(C(StatusLabel(l.Status))).Append(',')
              .Append(C(l.ConvertedCustomerId is null ? "Chưa" : "Rồi")).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "co-hoi-ban-hang.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateLeadDto(Input.FullName, Input.Phone, Input.Email, Input.Source, Input.Status, Input.AssignedToUserId, Input.BranchId));
        }
        else
        {
            await _svc.CreateAsync(new CreateLeadDto(Input.FullName, Input.Phone, Input.Email, Input.Source, Input.AssignedToUserId, Input.BranchId));
        }

        return new JsonResult(Result.Success("Đã lưu cơ hội bán hàng."));
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid id)
    {
        await _svc.ConvertAsync(id);
        TempData["ok"] = "Đã chuyển lead thành khách hàng.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá cơ hội.";
        return RedirectToPage();
    }
}
