using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Catalog;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

// Data khách hàng: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/customers/CustomersPage.tsx): 5 thẻ thống kê, chip phễu/chăm sóc kèm số đếm,
// 23 tiêu chí lọc, cột ghép 2 dòng (CSKH gần nhất, Phụ trách, Doanh thu/lần mua), dòng tổng cộng trang.
[Authorize(Policy = "customer.view")]
public class IndexModel : TkListPageModel
{
    private readonly ICustomerService _service;
    private readonly ICustomerTypeService _types;
    private readonly ICustomerSourceService _sources;
    private readonly ICustomerTagService _tags;
    private readonly IMarketTypeService _markets;
    private readonly IUserAdminService _users;
    public IndexModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets,
        IUserAdminService users)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _tags = tags;
        _markets = markets;
        _users = users;
    }

    // Thẻ thống kê + facet + phễu cho lần render đầu (DataTables lo bảng/search/paging qua OnGetData).
    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } =
        new([], [], [], [], [], [], [], [], [], []);
    public CustomerFunnelDto Funnel { get; private set; } = new(0, [], new(0, 0, 0, 0, 0, 0));
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<(int Code, string Name)> CustomerTypes { get; private set; } = [];

    // Bind cho offcanvas Save (create/update dùng chung; Id rỗng = tạo mới).
    [BindProperty] public CustomerFormInput Input { get; set; } = new();
    [BindProperty] public Guid? Id { get; set; }

    public async Task OnGetAsync()
    {
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
        Funnel = await _service.GetFunnelAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        CustomerTypes = (await _types.ListAsync()).OrderBy(t => t.SortOrder).Select(t => (t.Code, t.Name)).ToList();
        await LoadCatalogsAsync(this, _types, _sources, _tags, _markets);
    }

    // Danh mục cho offcanvas (Loại khách/Nguồn/Thẻ/Thị trường) — dùng chung Index + Details.
    internal static async Task LoadCatalogsAsync(PageModel page, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets)
    {
        page.ViewData["CustomerTypes"] = await types.ListAsync();
        page.ViewData["Sources"] = (await sources.ListAsync()).Select(s => s.Name).ToList();
        page.ViewData["Tags"] = (await tags.ListAsync()).Select(t => t.Name).ToList();
        page.ViewData["Markets"] = (await markets.ListAsync()).Select(m => m.Name).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — ĐỦ 23 tiêu chí của CustomerListFilter (không lọc ở client).</summary>
    private CustomerListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString().Trim();
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        decimal? M(string k) => decimal.TryParse(q[k], NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        DateTimeOffset? D(string k) =>
            DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        // Mốc "đến": người dùng chọn NGÀY → lấy hết ngày đó (nếu không thì mất dữ liệu trong ngày).
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

    /// <summary>Nguồn dữ liệu DataTables (server-side processing) — kèm đủ field để offcanvas sửa điền lại.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _service.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _service.GetStatsAsync();

        // Nhãn loại khách lấy từ danh mục (bám offcanvas); thiếu thì hiện mã.
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
            address = c.Address,
            segments = c.Segments,
            tags = c.Tags.Count > 0 ? c.Tags : (string.IsNullOrWhiteSpace(c.Tag) ? Array.Empty<string>() : new[] { c.Tag }),
            tag = c.Tags.Count > 0 ? string.Join(", ", c.Tags) : c.Tag,   // hiển thị ở lưới
            gender = c.Gender,
            dateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            idCardNumber = c.IdCardNumber,
            passportExpiry = c.PassportExpiry?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            marketGroup = c.MarketGroup,
            collaboratorName = c.CollaboratorName,
            assignedToNames = c.AssignedToNames,
            createdByName = c.CreatedByName,
            unitName = c.UnitName,
            taxCode = c.TaxCode,
            note = c.Note,
            purchaseCount = c.PurchaseCount,
            revenue = c.Revenue,
            lastCareAt = c.LastCareAt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            lastCareContent = c.LastCareContent,
            createdAt = c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI (bám dòng tổng hệ cũ: số lần mua + doanh thu).
        var pageSum = new
        {
            purchases = data.Sum(x => x.purchaseCount),
            revenue = data.Sum(x => x.revenue),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            pageSum,
        });
    }

    /// <summary>Lưu (tạo/sửa) từ offcanvas — AJAX, trả JSON.</summary>
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Phone))
        {
            return new JsonResult(Result.Error("Bắt buộc nhập số điện thoại"));
        }

        if (!ModelState.IsValid)
        {
            var err = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return new JsonResult(Result.Error(err ?? "Dữ liệu không hợp lệ."));
        }

        // SĐT là khoá check trùng: chặn nếu đã có khách khác cùng số (chuẩn hoá).
        var dup = await _service.FindByPhoneAsync(Input.Phone, Id);
        if (dup is not null)
        {
            return new JsonResult(Result.Error(
                $"Số điện thoại đã tồn tại — khách: {dup.FullName}{(string.IsNullOrEmpty(dup.Code) ? "" : $" ({dup.Code})")}"));
        }

        if (Id is Guid gid && gid != Guid.Empty)
        {
            await _service.UpdateAsync(gid, new UpdateCustomerDto(
                FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
                Source: Input.Source, Tag: Input.Tags.FirstOrDefault(), Tags: Input.Tags, Email: Input.Email, Address: Input.Address,
                DateOfBirth: TkDate.Day(Input.DateOfBirth), IdCardNumber: Input.IdCardNumber, PassportExpiry: TkDate.Day(Input.PassportExpiry),
                Gender: Input.Gender, City: Input.City, MarketGroup: Input.MarketGroup,
                CollaboratorName: Input.CollaboratorName,
                UnitName: Input.UnitName, TaxCode: Input.TaxCode, Note: Input.Note));
        }
        else
        {
            await _service.CreateAsync(new CreateCustomerDto(
                FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
                Source: Input.Source, Tag: Input.Tags.FirstOrDefault(), Tags: Input.Tags, Email: Input.Email, Address: Input.Address,
                DateOfBirth: TkDate.Day(Input.DateOfBirth), IdCardNumber: Input.IdCardNumber, PassportExpiry: TkDate.Day(Input.PassportExpiry),
                Gender: Input.Gender, City: Input.City, MarketGroup: Input.MarketGroup,
                CollaboratorName: Input.CollaboratorName,
                UnitName: Input.UnitName, TaxCode: Input.TaxCode, Note: Input.Note));
        }

        return new JsonResult(Result.Success("Đã lưu khách hàng."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách hàng.";
        return RedirectToPage();
    }
}
