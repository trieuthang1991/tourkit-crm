using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.MarketTypes;

// Danh mục thị trường: chuẩn hoá tk.grid + tk-surface (như ServiceItems) — context menu + full-height,
// đồng bộ cả nhóm catalog. Vẫn giữ Items cho ô "Thuộc nhóm cha" trong form (server-render options).
[Authorize(Policy = "market.view")]
public class IndexModel : TkListPageModel
{
    private readonly IMarketTypeService _svc;
    private readonly TourKit.Api.Services.MarketDirectory _dir;
    public IndexModel(IMarketTypeService svc, TourKit.Api.Services.MarketDirectory dir)
    {
        _svc = svc;
        _dir = dir;
    }

    public IReadOnlyList<MarketTypeDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync() => Items = await _dir.ListAsync();

    public string? ParentName(Guid? parentId) =>
        parentId is null ? null : Items.FirstOrDefault(x => x.Id == parentId)?.Name;

    /// <summary>Nguồn tk.grid server-side — danh mục nhỏ, phân trang trong bộ nhớ + tra tên nhóm cha.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var all = await _dir.ListAsync();
        var kw = dt.Keyword?.ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862
        var loc = kw is null
            ? all
            : all.Where(x => x.Name.ToLower().Contains(kw)).ToList();
#pragma warning restore CA1304, CA1311, CA1862
        var ten = all.ToDictionary(x => x.Id, x => x.Name);
        var data = loc.Skip((dt.Page - 1) * dt.Size).Take(dt.Size).Select(x => new
        {
            id = x.Id,
            name = x.Name,
            parentId = x.ParentId,
            parentName = x.ParentId is Guid pid && ten.TryGetValue(pid, out var pn) ? pn : "—",
            sortOrder = x.SortOrder,
        });
        return DtJson(dt.Draw, all.Count, loc.Count, data);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateMarketTypeDto(Input.Name, Input.ParentId, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateMarketTypeDto(Input.Name, Input.ParentId, Input.SortOrder));
        }

        await _dir.InvalidateAsync(); // dropdown thị trường ở các màn khác phải thấy thay đổi ngay
        return new JsonResult(Result.Success("Đã lưu thị trường."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        await _dir.InvalidateAsync();
        TempData["ok"] = "Đã xoá thị trường.";
        return RedirectToPage();
    }
}
