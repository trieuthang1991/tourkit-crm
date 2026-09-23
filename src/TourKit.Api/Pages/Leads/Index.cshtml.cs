using TourKit.Api.Services;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Crm;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Leads;

// Khách tiềm năng (Lead) — số khách thô được chia cho sale, hệ cũ để ở menu "Chia số Sale".
// ĐỪNG NHẦM với màn Cơ hội bán hàng (/co-hoi, SalesOpportunity): cơ hội là nhu cầu đã có số khách
// và giá, còn ở đây mới chỉ có tên với số điện thoại.
// DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/leads/LeadsPage.tsx): 7 thẻ KPI, 7 tiêu chí lọc đẩy xuống SQL,
// cột ghép (Cơ hội = tên + chi nhánh · Liên hệ = SĐT + email), nút Chuyển thành KH, export CSV.
[Authorize(Policy = "lead.view")]
public class IndexModel : TkListPageModel
{
    private readonly ILeadService _svc;
    private readonly UserDirectory _users;
    private readonly IBranchService _branches;
    private readonly ICustomerSourceService _sources;

    public IndexModel(ILeadService svc, UserDirectory users, IBranchService branches, ICustomerSourceService sources)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
        _sources = sources;
    }

    public LeadStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    /// <summary>Nguồn cho Ô LỌC: danh mục + những giá trị đang thật sự có trong dữ liệu.</summary>
    public IReadOnlyList<string> Sources { get; private set; } = [];

    /// <summary>Nguồn cho Ô NHẬP: CHỈ danh mục chuẩn — đây là chỗ chặn không cho sinh thêm giá trị mới.</summary>
    public IReadOnlyList<string> SourceCatalog { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Branches { get; private set; } = [];

    /// <summary>
    /// Chiến dịch cần lọc sẵn khi vào từ màn Chia số Sale (<c>/khach-tiem-nang?campaignId=…</c>).
    ///
    /// Phải nạp vào một Ô LỌC ẨN chứ không đọc thẳng ở handler dữ liệu: lưới gọi URL tương đối
    /// '?handler=Data' nên nó THAY THẾ toàn bộ query string — campaignId của trang không đi kèm.
    /// </summary>
    public Guid? LocCampaignId { get; private set; }

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

        /// <summary>Nhu cầu khách tự nêu, bằng lời của họ.</summary>
        public string? Note { get; set; }

        /// <summary>
        /// Nguồn chi tiết dán nguyên dạng đường dẫn (<c>…?utm_source=zns&amp;utm_medium=sms</c>).
        /// Tách ra thành các trường ở <see cref="LeadAttribution.TuChuoiTruyVan"/> — bắt người dùng
        /// điền từng ô utm là cách chắc chắn nhất để không ai điền.
        /// </summary>
        public string? AttributionRaw { get; set; }
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
        LocCampaignId = Guid.TryParse(Request.Query["campaignId"], out var cd) ? cd : null;

        SourceCatalog = (await _sources.ListAsync()).Select(x => x.Name).ToList();

        // Ô LỌC gộp danh mục với giá trị đang có trong dữ liệu, KHÔNG chỉ lấy danh mục:
        //  - danh mục mới thêm nhưng chưa có cơ hội nào vẫn lọc được;
        //  - dữ liệu cũ (gõ tay, có thể sai chính tả) vẫn tìm lại được thay vì biến mất khỏi bộ lọc.
        Sources = SourceCatalog
            .Union((await _svc.GetFilterOptionsAsync()).Sources, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.CurrentCulture)
            .ToList();
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
            CreatedByUserId: G("createdByUserId"),
            CampaignId: G("campaignId"));
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
            note = l.Note,
            branchName = l.BranchId is Guid b && branchNames.TryGetValue(b, out var bn) ? bn : null,
            convertedCustomerId = l.ConvertedCustomerId,
        }).ToList();

        return DtJson(dt.Draw, stats.Total, result.Total, items);
    }

    /// <summary>
    /// MỘT cơ hội theo id, cùng hình dạng với dòng lưới để mở thẳng offcanvas sửa.
    ///
    /// Dùng cho đường dẫn sâu <c>/khach-tiem-nang?mo={id}</c> — người nhận thông báo "@nhắc bạn trong Cơ hội
    /// bán hàng" bấm vào phải mở ĐÚNG cơ hội đó. Không thể lấy từ dữ liệu lưới đã tải: cơ hội cần mở
    /// thường không nằm ở trang đầu.
    /// </summary>
    public async Task<IActionResult> OnGetOneAsync(Guid id)
    {
        LeadDto lead;
        try
        {
            lead = await _svc.GetAsync(id);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        var userNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var branchNames = (await _branches.ListAsync()).ToDictionary(b => b.Id, b => b.Name);

        return new JsonResult(new
        {
            id = lead.Id,
            fullName = lead.FullName,
            phone = lead.Phone,
            email = lead.Email,
            source = lead.Source,
            status = (int)lead.Status,
            statusLabel = StatusLabel(lead.Status),
            statusColor = StatusColor(lead.Status),
            assignedToUserId = lead.AssignedToUserId,
            assigneeName = lead.AssignedToUserId is Guid u && userNames.TryGetValue(u, out var un) ? un : null,
            branchId = lead.BranchId,
            note = lead.Note,
            branchName = lead.BranchId is Guid b && branchNames.TryGetValue(b, out var bn) ? bn : null,
            convertedCustomerId = lead.ConvertedCustomerId,
        });
    }

    /// <summary>
    /// Một CỘT Kanban: lấy TOP N lead của đúng 1 trạng thái (kèm bộ lọc đang áp), có phân trang
    /// "Tải thêm". KHÔNG get-all — mỗi cột chỉ tải trang của riêng nó.
    /// </summary>
    public async Task<IActionResult> OnGetKanbanColumnAsync(int status, int page = 1, int size = 15)
    {
        if (size is < 1 or > 50)
        {
            size = 15;
        }

        var f = BuildFilter(string.IsNullOrWhiteSpace(Request.Query["q"]) ? null : Request.Query["q"].ToString());
        var result = await _svc.ListAsync(page, size, f with { Status = status });

        var userNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var cards = result.Items.Select(l => new
        {
            id = l.Id,
            fullName = l.FullName,
            phone = l.Phone,
            email = l.Email,
            source = l.Source,
            status = (int)l.Status,
            assigneeName = l.AssignedToUserId is Guid u && userNames.TryGetValue(u, out var un) ? un : null,
            convertedCustomerId = l.ConvertedCustomerId,
        });

        return new JsonResult(new { total = result.Total, page, size, hasMore = page * size < result.Total, cards });
    }

    /// <summary>Kéo–thả đổi trạng thái trên Kanban (giữ nguyên các field khác). Tham số tên `to` vì
    /// component chung tk.kanban gửi cột đích dưới tên đó; menu trên dòng lưới đi lối
    /// <see cref="OnPostSetStatusAsync"/> riêng.</summary>
    public async Task<IActionResult> OnPostMoveAsync(Guid id, int to)
    {
        var status = to;
        if (!Enum.IsDefined(typeof(LeadStatus), status))
        {
            return new JsonResult(Result.Error("Trạng thái không hợp lệ."));
        }

        try
        {
            var lead = await _svc.GetAsync(id);
            await _svc.UpdateAsync(id, new UpdateLeadDto(
                lead.FullName, lead.Phone, lead.Email, lead.Source, (LeadStatus)status, lead.AssignedToUserId, lead.BranchId));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã chuyển trạng thái."));
    }

    /// <summary>Đổi nhanh trạng thái từ menu trên dòng lưới — tái dùng ĐÚNG thao tác Get+Update mà
    /// Kanban (<see cref="OnPostMoveAsync"/>) đang dùng, chỉ khác nơi gọi (JSON toast thay vì kéo–thả).</summary>
    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, int status)
    {
        if (!Enum.IsDefined(typeof(LeadStatus), status))
        {
            return new JsonResult(Result.Error("Trạng thái không hợp lệ."));
        }

        try
        {
            var lead = await _svc.GetAsync(id);
            await _svc.UpdateAsync(id, new UpdateLeadDto(
                lead.FullName, lead.Phone, lead.Email, lead.Source, (LeadStatus)status, lead.AssignedToUserId, lead.BranchId));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success($"Đã đổi trạng thái thành \"{StatusLabel((LeadStatus)status)}\"."));
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
        return File(bytes, "text/csv", "khach-tiem-nang.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        // Ô "Nguồn chi tiết" nhận nguyên đường dẫn chiến dịch rồi tách ra. Để TRỐNG thì truyền null
        // chứ không phải một bản rỗng: bản rỗng sẽ GHI ĐÈ lên phần nguồn mà form thu lead đã ghi
        // lúc khách để lại thông tin — mất dấu vết vì một lần sửa tên khách.
        var nguonChiTiet = string.IsNullOrWhiteSpace(Input.AttributionRaw)
            ? null
            : LeadAttribution.TuChuoiTruyVan(Input.AttributionRaw);

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateLeadDto(
                Input.FullName, Input.Phone, Input.Email, Input.Source, Input.Status,
                Input.AssignedToUserId, Input.BranchId, Input.Note, nguonChiTiet));
        }
        else
        {
            await _svc.CreateAsync(new CreateLeadDto(
                Input.FullName, Input.Phone, Input.Email, Input.Source,
                Input.AssignedToUserId, Input.BranchId, Input.Note, nguonChiTiet));
        }

        return new JsonResult(Result.Success("Đã lưu khách tiềm năng."));
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid id, Guid? assignedToUserId)
    {
        var ketQua = await _svc.ConvertAsync(id, assignedToUserId);

        // Nói rõ khi NỐI vào khách sẵn có thay vì tạo mới: người bấm nút đang đinh ninh vừa tạo ra
        // một hồ sơ khách hàng, im lặng thì họ đi tìm một bản ghi không tồn tại.
        TempData["ok"] = ketQua.DaGanVaoKhachSanCo
            ? "Số điện thoại đã có hồ sơ khách hàng — đã nối vào hồ sơ đó, không tạo bản sao."
            : "Đã chuyển thành khách hàng.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách tiềm năng.";
        return RedirectToPage();
    }
}
