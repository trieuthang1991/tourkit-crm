using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Pages.ServiceItems;

// Danh mục dịch vụ: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/services/ServiceItemsPage.tsx): cột Mã, Tên, Loại (nhãn SERVICE_CATEGORY), Trạng thái.
// IServiceItemService.ListAsync CHỈ nhận (page, size) → không có tiêu chí lọc nào để đẩy xuống SQL.
[Authorize(Policy = "service.view")]
public class IndexModel : TkListPageModel
{
    private readonly IServiceItemService _svc;
    public IndexModel(IServiceItemService svc) => _svc = svc;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public int Category { get; set; } = 1;
        public int Status { get; set; } = 1;
    }

    /// <summary>Nhóm dịch vụ — bám SERVICE_CATEGORY của bản cũ (1..7).</summary>
    public static readonly (int Value, string Label)[] CategoryOptions =
    [
        (1, "Khách sạn"), (2, "Vận chuyển"), (3, "Nhà hàng"), (4, "HDV"), (5, "Hàng không"), (6, "Visa"), (7, "Khác"),
    ];

    public static string CategoryLabel(int c)
    {
        foreach (var o in CategoryOptions)
        {
            if (o.Value == c)
            {
                return o.Label;
            }
        }

        return c.ToString(CultureInfo.InvariantCulture);
    }

    public static string StatusLabel(int s) => s == 1 ? "Hoạt động" : "Ngừng";

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + đủ field cho offcanvas sửa.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, dt.Keyword);

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code,
            name = x.Name,
            category = x.Category,
            categoryLabel = CategoryLabel(x.Category),
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
        });

        return DtJson(dt.Draw, result.Total, result.Total, data);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateServiceItemDto(Input.Name, Input.Category, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateServiceItemDto(Input.Code, Input.Name, Input.Category, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu dịch vụ."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá dịch vụ.";
        return RedirectToPage();
    }
}
