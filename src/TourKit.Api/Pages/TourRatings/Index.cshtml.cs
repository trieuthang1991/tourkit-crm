using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Pages.TourRatings;

// Đánh giá tour: DataTables SERVER-SIDE + GIỮ ĐỦ cột bản cũ
// (web/src/features/ratings/TourRatingsPage.tsx — ResourcePage: Chuyến đi · Khách hàng · Số sao · Trạng thái)
// + cột SĐT/Nhận xét đã có ở bản Razor. Service ListAsync(page,size) KHÔNG nhận filter/keyword nên
// màn này không có thanh lọc và ô tìm của DataTables bị ẩn (không lọc client, không get-all).
[Authorize(Policy = "rating.view")]
public class IndexModel : TkListPageModel
{
    private readonly ITourRatingService _svc;
    private readonly IDepartureService _departures;
    public IndexModel(ITourRatingService svc, IDepartureService departures)
    {
        _svc = svc;
        _departures = departures;
    }

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int Stars { get; set; } = 5;
        public string? Comment { get; set; }
        public int Status { get; set; } = 1;
    }

    public void OnGet()
    {
        // Không có dữ liệu dựng sẵn: bảng nạp qua ?handler=Data.
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, dt.Keyword);

        // Tra TÊN chuyến cho các đánh giá xuất hiện TRONG TRANG (thay vì hiện GUID thô ở cột Chuyến đi).
        // Chỉ tra đúng các id có mặt — không nạp cả bảng chuyến.
        var depIds = result.Items.Where(r => r.TourDepartureId is not null)
            .Select(r => r.TourDepartureId!.Value).Distinct().ToList();
        var depNames = new Dictionary<Guid, string>();
        foreach (var id in depIds)
        {
            try { var d = await _departures.GetAsync(id); depNames[id] = $"{d.Code} — {d.Title}"; }
            catch (Exception) { /* chuyến đã xoá → để trống, cột hiện "—" */ }
        }

        var items = result.Items.Select(r => new
        {
            id = r.Id,
            tourDepartureId = r.TourDepartureId,
            departureName = r.TourDepartureId is Guid g && depNames.TryGetValue(g, out var n) ? n : null,
            orderId = r.OrderId,
            customerName = r.CustomerName,
            customerPhone = r.CustomerPhone,
            stars = r.Stars,
            comment = r.Comment,
            status = r.Status,
        }).ToList();

        return DtJson(dt.Draw, result.Total, result.Total, items);
    }

    /// <summary>Xuất CSV (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var result = await _svc.ListAsync(1, max);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Chuyến đi,Khách hàng,SĐT,Số sao,Nhận xét,Trạng thái");
        foreach (var r in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(r.TourDepartureId?.ToString())).Append(',').Append(C(r.CustomerName)).Append(',')
              .Append(C(r.CustomerPhone)).Append(',')
              .Append(r.Stars.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(C(r.Comment)).Append(',')
              .Append(r.Status.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "danh-gia-tour.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateTourRatingDto(Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateTourRatingDto(null, null, Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu đánh giá tour."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá đánh giá tour.";
        return RedirectToPage();
    }
}
