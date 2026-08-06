using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.Shared;

/// <summary>
/// Base cho màn danh sách phân trang từ server. Hiểu CẢ hai kiểu request:
/// DataTables (start/length/search[value]) và Tabulator qua tk.grid (page/size/q).
/// Nhờ vậy chuyển một màn từ DataTables sang tk.grid chỉ cần đổi hàm trả JSON.
/// </summary>
public abstract class TkListPageModel : PageModel
{
    protected sealed record DtRequest(int Draw, int Start, int Length, string Search, int? GridPage, int? GridSize)
    {
        public int Page => GridPage ?? (Length <= 0 ? 1 : (Start / Length) + 1);
        public int Size => GridSize ?? (Length <= 0 ? 20 : Length);
        public string? Keyword => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }

    protected DtRequest ParseDataTables()
    {
        var q = Request.Query;
        int P(string k, int def) => int.TryParse(q[k], out var n) ? n : def;
        int? N(string k, int max) => int.TryParse(q[k], out var n) && n > 0 && n <= max ? n : null;

        // tk.grid gửi page/size/q; DataTables gửi start/length/search[value]. Ưu tiên cái nào có mặt.
        var search = q["search[value]"].ToString();
        if (string.IsNullOrWhiteSpace(search)) { search = q["q"].ToString(); }
        return new DtRequest(P("draw", 0), P("start", 0), P("length", 20), search,
            N("page", 100_000), N("size", 500));
    }

    /// <summary>JSON đúng contract DataTables server-side.</summary>
    protected JsonResult DtJson(int draw, int recordsTotal, int recordsFiltered, object data)
        => new(new { draw, recordsTotal, recordsFiltered, data });

    /// <summary>
    /// JSON phục vụ ĐỒNG THỜI Tabulator (tk.grid: last_page + data) và DataTables
    /// (draw/recordsTotal/recordsFiltered/data) — hai bộ khoá không đụng nhau nên một payload
    /// dùng được cho cả hai, chuyển màn sang lưới mới không sợ vỡ màn cũ hay test cũ.
    /// <paramref name="extra"/> để gắn thêm số liệu riêng của màn (ví dụ pageSum của dòng tổng).
    /// </summary>
    protected JsonResult GridJson(DtRequest dt, int recordsTotal, int recordsFiltered, object data,
        IDictionary<string, object?>? extra = null)
    {
        var size = dt.Size <= 0 ? 20 : dt.Size;
        var payload = new Dictionary<string, object?>
        {
            ["last_page"] = Math.Max(1, (int)Math.Ceiling(recordsFiltered / (double)size)),
            ["last_row"] = recordsFiltered,
            ["draw"] = dt.Draw,
            ["recordsTotal"] = recordsTotal,
            ["recordsFiltered"] = recordsFiltered,
            ["data"] = data,
        };
        if (extra is not null)
        {
            foreach (var kv in extra) { payload[kv.Key] = kv.Value; }
        }
        return new JsonResult(payload);
    }
}
