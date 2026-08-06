using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Crm;

namespace TourKit.Api.Pages.TourRatings;

// Feedback theo Tour: tổng hợp đánh giá GOM THEO CHUYẾN (số lượt + sao trung bình) — bám module
// ListForTour của hệ cũ. Khác màn "Đánh giá tour" (danh sách phẳng từng lượt). GROUP BY chạy ở SQL
// trong service; trang chỉ tra TÊN chuyến cho đúng các dòng xuất hiện trong trang.
[Authorize(Policy = "rating.view")]
public class ByTourModel : TkListPageModel
{
    private readonly ITourRatingService _svc;
    private readonly IDepartureService _departures;
    public ByTourModel(ITourRatingService svc, IDepartureService departures)
    {
        _svc = svc;
        _departures = departures;
    }

    public void OnGet()
    {
        // Không dựng sẵn dữ liệu: bảng nạp qua ?handler=Data.
    }

    /// <summary>Nguồn DataTables server-side: mỗi lần chỉ trả đúng 1 trang các chuyến.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListByTourAsync(dt.Page, dt.Size);

        // Tra TÊN cho đúng các chuyến TRONG TRANG (thay vì GUID thô), không nạp cả bảng chuyến.
        var depNames = new Dictionary<Guid, string>();
        foreach (var id in result.Items.Select(r => r.TourDepartureId).Distinct())
        {
            try { var d = await _departures.GetAsync(id); depNames[id] = $"{d.Code} — {d.Title}"; }
            catch (Exception) { /* chuyến đã xoá → để trống, cột hiện "—" */ }
        }

        var items = result.Items.Select(r => new
        {
            tourDepartureId = r.TourDepartureId,
            departureName = depNames.TryGetValue(r.TourDepartureId, out var n) ? n : null,
            ratingCount = r.RatingCount,
            averageStars = r.AverageStars,
        }).ToList();

        return DtJson(dt.Draw, result.Total, result.Total, items);
    }
}
