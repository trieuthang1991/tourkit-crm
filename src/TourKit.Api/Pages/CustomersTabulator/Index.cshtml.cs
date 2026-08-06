using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Services;
using TourKit.Application.Catalog;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.CustomersTabulator;

// BẢN THỬ NGHIỆM: cùng dữ liệu/nghiệp vụ với Customers/Index (ICustomerService) nhưng render bằng
// Tabulator (MIT, mã nguồn mở) thay DataTables — để so sánh look & UX. KHÔNG đụng trang cũ.
[Authorize(Policy = "customer.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerService _service;
    private readonly ICustomerTypeService _types;
    private readonly ICustomerSourceService _sources;
    private readonly UserDirectory _users;

    public IndexModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, UserDirectory users)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _users = users;
    }

    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } = new([], [], [], [], [], [], [], [], [], []);
    public IReadOnlyList<(int Code, string Name)> CustomerTypes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
        CustomerTypes = (await _types.ListAsync()).OrderBy(t => t.SortOrder).Select(t => (t.Code, t.Name)).ToList();
    }

    /// <summary>Bộ lọc từ query — cùng khoá với trang gốc để đẩy hết xuống server (không lọc client).</summary>
    private CustomerListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString().Trim();
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        decimal? M(string k) => decimal.TryParse(q[k], NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        DateTimeOffset? D(string k) =>
            DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        DateTimeOffset? DEnd(string k) =>
            D(k) is { } d ? (d.TimeOfDay == TimeSpan.Zero ? d.AddDays(1).AddTicks(-1) : d) : null;

        return new CustomerListFilter(
            Q: keyword,
            CustomerType: I("customerType"),
            Source: S("source"), City: S("city"), Gender: S("gender"), MarketGroup: S("marketGroup"),
            Collaborator: S("collaborator"), Campaign: S("campaign"), Branch: S("branch"), Group: S("group"),
            Department: S("department"), Segment: S("segment"), Tag: S("tag"), AssignedTo: S("assignedTo"),
            CreatedBy: S("createdBy"),
            CreatedFrom: D("createdFrom"), CreatedTo: DEnd("createdTo"),
            CareFrom: D("careFrom"), CareTo: DEnd("careTo"),
            RevenueFrom: M("revenueFrom"), RevenueTo: M("revenueTo"),
            BirthdayMonth: I("birthdayMonth"),
            PurchaseBucket: S("purchaseBucket"), NotContactedBucket: S("notContactedBucket"));
    }

    /// <summary>Nguồn dữ liệu cho Tabulator (remote pagination): nhận page/size/q, trả {last_page,data,pageSum}.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var page = int.TryParse(Request.Query["page"], out var p) && p > 0 ? p : 1;
        var size = int.TryParse(Request.Query["size"], out var s) && s is > 0 and <= 200 ? s : 20;
        var keyword = string.IsNullOrWhiteSpace(Request.Query["q"]) ? null : Request.Query["q"].ToString().Trim();

        var result = await _service.ListAsync(page, size, BuildFilter(keyword));
        var stats = await _service.GetStatsAsync();
        var typeNames = (await _types.ListAsync()).GroupBy(t => t.Code).ToDictionary(g => g.Key, g => g.First().Name);

        var data = result.Items.Select(c => new
        {
            id = c.Id,
            code = c.Code,
            fullName = c.FullName,
            phone = c.Phone,
            email = c.Email,
            customerType = c.CustomerType,
            customerTypeName = typeNames.TryGetValue(c.CustomerType, out var tn)
                ? tn
                : c.CustomerType.ToString(CultureInfo.InvariantCulture),
            source = c.Source,
            city = c.City,
            segments = c.Segments,
            tag = c.Tags.Count > 0 ? string.Join(", ", c.Tags) : c.Tag,
            marketGroup = c.MarketGroup,
            collaboratorName = c.CollaboratorName,
            assignedToNames = c.AssignedToNames,
            createdByName = c.CreatedByName,
            purchaseCount = c.PurchaseCount,
            revenue = c.Revenue,
            lastCareAt = c.LastCareAt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            lastCareContent = c.LastCareContent,
            createdAt = c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        }).ToList();

        var lastPage = Math.Max(1, (int)Math.Ceiling(result.Total / (double)size));

        return new JsonResult(new
        {
            last_page = lastPage,
            last_row = result.Total,
            recordsTotal = stats.Total,
            data,
            pageSum = new { purchases = data.Sum(x => x.purchaseCount), revenue = data.Sum(x => x.revenue) },
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách hàng.";
        return RedirectToPage();
    }
}
