using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Pages.CustomerCommissionRules;

// Danh sách HH theo loại khách: DataTables SERVER-SIDE (không get-all) + giữ đủ thông tin bản cũ
// (web/src/features/customerCommissionRules/CustomerCommissionRulesPage.tsx): 4 KPI, lọc loại khách +
// trạng thái (CustomerCommissionRuleListFilter), cột Loại khách / Hoa hồng (%) / Trạng thái, Sửa + Xoá.
// ListAsync KHÔNG nhận từ khoá → ẩn ô search mặc định của DataTables.
// Update chỉ đổi Percentage/Status (UpdateCustomerCommissionRuleDto không nhận CustomerType).
[Authorize(Policy = "commission.view")]
public class IndexModel : TkListPageModel
{
    private readonly ICustomerCommissionRuleService _svc;
    private readonly ICustomerTypeService _types;
    public IndexModel(ICustomerCommissionRuleService svc, ICustomerTypeService types)
    {
        _svc = svc;
        _types = types;
    }

    public IReadOnlyList<CustomerTypeDto> Types { get; private set; } = [];
    public CustomerCommissionRuleStatsDto Stats { get; private set; } = new(0, 0, 0, 0m);

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public int CustomerType { get; set; }
        public decimal Percentage { get; set; }
        public int Status { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        Types = await _types.ListAsync();
        Stats = await _svc.GetStatsAsync();
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang, mọi tiêu chí đẩy xuống service.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var q = Request.Query;
        int? customerType = int.TryParse(q["customerType"], out var ct) ? ct : null;
        int? status = int.TryParse(q["status"], out var st) ? st : null;

        var result = await _svc.ListAsync(dt.Page, dt.Size, new CustomerCommissionRuleListFilter(customerType, status));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            customerType = x.CustomerType,
            customerTypeName = x.CustomerTypeName ?? ("#" + x.CustomerType.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            percentage = x.Percentage,
            status = x.Status,
            statusLabel = x.Status == 1 ? "Đang áp dụng" : "Tạm ngừng",
            statusColor = x.Status == 1 ? "success" : "secondary",
        });

        return DtJson(dt.Draw, stats.Total, result.Total, data);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCustomerCommissionRuleDto(Input.Percentage, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateCustomerCommissionRuleDto(Input.CustomerType, Input.Percentage, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu hoa hồng theo loại khách."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá hoa hồng theo loại khách.";
        return RedirectToPage();
    }
}
