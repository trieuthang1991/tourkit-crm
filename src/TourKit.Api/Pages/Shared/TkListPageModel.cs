using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.Shared;

/// <summary>Base cho màn list DataTables server-side: parse request + trả JSON đúng contract DataTables.</summary>
public abstract class TkListPageModel : PageModel
{
    protected sealed record DtRequest(int Draw, int Start, int Length, string Search)
    {
        public int Page => Length <= 0 ? 1 : (Start / Length) + 1;
        public int Size => Length <= 0 ? 20 : Length;
        public string? Keyword => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }

    protected DtRequest ParseDataTables()
    {
        var q = Request.Query;
        int P(string k, int def) => int.TryParse(q[k], out var n) ? n : def;
        return new DtRequest(P("draw", 0), P("start", 0), P("length", 20), q["search[value]"].ToString());
    }

    /// <summary>JSON đúng contract DataTables server-side.</summary>
    protected JsonResult DtJson(int draw, int recordsTotal, int recordsFiltered, object data)
        => new(new { draw, recordsTotal, recordsFiltered, data });
}
