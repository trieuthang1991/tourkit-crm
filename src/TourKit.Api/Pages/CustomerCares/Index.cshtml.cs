using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.CustomerCares;

// Chăm sóc khách hàng: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/care/CustomerCaresPage.tsx): 5 thẻ KPI, 4 tiêu chí lọc (từ khoá · khách ·
// người phụ trách · trạng thái dạng chip), cột ghép Nội dung (tiêu đề + chi tiết), cột Phản hồi, export CSV.
[Authorize(Policy = "care.view")]
public class IndexModel : TkListPageModel
{
    private readonly ICustomerCareService _svc;
    private readonly IUserAdminService _users;
    private readonly ICustomerService _customers;

    public IndexModel(ICustomerCareService svc, IUserAdminService users, ICustomerService customers)
    {
        _svc = svc;
        _users = users;
        _customers = customers;
    }

    public CustomerCareStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public Guid CustomerId { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        public string? Detail { get; set; }
        public DateTimeOffset? RemindAt { get; set; }
        public string? Feedback { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public int Status { get; set; }
    }

    // Trạng thái bám CARE_STATUS hệ cũ: 0 Mới · 1 Đang xử lý · 2 Hoàn thành.
    public static string StatusLabel(int s) => s switch
    {
        0 => "Mới",
        1 => "Đang xử lý",
        2 => "Hoàn thành",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        0 => "warning",
        1 => "info",
        2 => "success",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Nguồn cho Select2 ajax chọn khách (top 20 theo Q). Trả [{id,text}].</summary>
    public async Task<IActionResult> OnGetCustomerSearchAsync(string? q)
    {
        var result = await _customers.ListAsync(1, 20, new CustomerListFilter(Q: q));
        var items = result.Items.Select(c => new { id = c.Id, text = $"{c.FullName}{(string.IsNullOrEmpty(c.Phone) ? "" : " - " + c.Phone)}" });
        return new JsonResult(new { results = items });
    }

    /// <summary>Dựng bộ lọc từ query — đúng 4 tiêu chí CustomerCareListFilter hỗ trợ.</summary>
    private CustomerCareListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        return new CustomerCareListFilter(
            Q: keyword,
            CustomerId: G("customerId"),
            AssignedToUserId: G("assignedToUserId"),
            Status: I("status"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();
        var now = DateTimeOffset.UtcNow;

        var items = result.Items.Select(c => new
        {
            id = c.Id,
            customerId = c.CustomerId,
            customerName = c.CustomerName,
            title = c.Title,
            detail = c.Detail,
            remindAt = c.RemindAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            remindAtText = c.RemindAt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            overdue = c.RemindAt is DateTimeOffset r && r < now && c.Status != 2,
            feedback = c.Feedback,
            assignedToUserId = c.AssignedToUserId,
            assigneeName = c.AssigneeName,
            status = c.Status,
            statusLabel = StatusLabel(c.Status),
            statusColor = StatusColor(c.Status),
        }).ToList();

        return DtJson(dt.Draw, stats.Total, result.Total, items);
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Khách hàng,Tiêu đề,Nội dung,Người phụ trách,Nhắc hẹn,Phản hồi,Trạng thái");
        foreach (var c in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(c.CustomerName)).Append(',').Append(C(c.Title)).Append(',').Append(C(c.Detail)).Append(',')
              .Append(C(c.AssigneeName)).Append(',')
              .Append(C(c.RemindAt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(c.Feedback)).Append(',').Append(C(StatusLabel(c.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "cham-soc-khach-hang.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        // Npgsql chỉ nhận DateTimeOffset offset 0.
        var remindAt = Input.RemindAt?.ToUniversalTime();

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCustomerCareDto(Input.Title, Input.Detail, remindAt, Input.Feedback, Input.AssignedToUserId, Input.Status));
        }
        else
        {
            if (Input.CustomerId == Guid.Empty)
            {
                return new JsonResult(Result.Error("Bắt buộc chọn khách hàng."));
            }

            await _svc.CreateAsync(new CreateCustomerCareDto(Input.CustomerId, Input.Title, Input.Detail, remindAt, Input.AssignedToUserId, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu lịch chăm sóc."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá lịch chăm sóc.";
        return RedirectToPage();
    }
}
