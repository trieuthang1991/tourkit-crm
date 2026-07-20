using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Pages.Agents;

// Danh sách đại lý B2B: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/agents/AgentsPage.tsx): 4 thẻ thống kê, tab trạng thái kèm số đếm, tìm kiếm,
// cột ghép 2 dòng (Đại lý / Liên hệ), dòng tổng cộng trang, export CSV.
[Authorize(Policy = "agent.view")]
public class IndexModel : TkListPageModel
{
    private readonly IAgentService _svc;
    public IndexModel(IAgentService svc) => _svc = svc;

    public AgentStatsDto Stats { get; private set; } = new(0, 0, 0, 0m);

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public decimal CreditLimit { get; set; }
        public int Status { get; set; } = 1;
    }

    public static string StatusLabel(int s) => s == 1 ? "Hoạt động" : "Ngừng";

    public async Task OnGetAsync() => Stats = await _svc.GetStatsAsync();

    /// <summary>Dựng bộ lọc từ query — ĐỦ 2 tiêu chí của AgentListFilter (không lọc ở client).</summary>
    private AgentListFilter BuildFilter(string? keyword)
    {
        var status = int.TryParse(Request.Query["status"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : (int?)null;
        return new AgentListFilter(Q: keyword, Status: status);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + đủ field cho offcanvas sửa.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(a => new
        {
            id = a.Id,
            code = a.Code,
            name = a.Name,
            contactPerson = a.ContactPerson,
            phone = a.Phone,
            email = a.Email,
            taxCode = a.TaxCode,
            address = a.Address,
            creditLimit = a.CreditLimit,
            status = a.Status,
            statusLabel = StatusLabel(a.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI — hạn mức tín dụng.
        var pageSum = new { creditLimit = data.Sum(x => x.creditLimit) };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            pageSum,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng để không sập).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã,Tên đại lý,Người liên hệ,SĐT,Email,MST,Địa chỉ,Hạn mức,Trạng thái");
        foreach (var a in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(a.Code)).Append(',').Append(C(a.Name)).Append(',').Append(C(a.ContactPerson)).Append(',')
              .Append(C(a.Phone)).Append(',').Append(C(a.Email)).Append(',').Append(C(a.TaxCode)).Append(',')
              .Append(C(a.Address)).Append(',')
              .Append(a.CreditLimit.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(a.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "dai-ly.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateAgentDto(Input.Code, Input.Name, Input.ContactPerson, Input.Phone, Input.Email, Input.TaxCode, Input.Address, Input.CreditLimit, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateAgentDto(Input.Code, Input.Name, Input.ContactPerson, Input.Phone, Input.Email, Input.TaxCode, Input.Address, Input.CreditLimit, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu đại lý."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá đại lý.";
        return RedirectToPage();
    }
}
