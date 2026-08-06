using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Services;
using TourKit.Api.Web;
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
    private readonly ICustomerTagService _tags;
    private readonly IMarketTypeService _markets;
    private readonly UserDirectory _users;

    public IndexModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets,
        UserDirectory users)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _tags = tags;
        _markets = markets;
        _users = users;
    }

    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } = new([], [], [], [], [], [], [], [], [], []);
    public IReadOnlyList<(int Code, string Name)> CustomerTypes { get; private set; } = [];

    // Bind cho offcanvas Thêm/Sửa (dùng chung; Id rỗng = tạo mới) — giống trang Customers/Index.
    [BindProperty] public Customers.CustomerFormInput Input { get; set; } = new();
    [BindProperty] public Guid? Id { get; set; }

    public async Task OnGetAsync()
    {
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
        CustomerTypes = (await _types.ListAsync()).OrderBy(t => t.SortOrder).Select(t => (t.Code, t.Name)).ToList();
        // Danh mục cho offcanvas — dùng lại đúng hàm của trang gốc để không lệch nghiệp vụ.
        await Customers.IndexModel.LoadCatalogsAsync(this, _types, _sources, _tags, _markets);
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
            // Field cho offcanvas SỬA điền lại (không hiển thị ở lưới).
            tags = c.Tags.Count > 0 ? c.Tags : (string.IsNullOrWhiteSpace(c.Tag) ? [] : new[] { c.Tag }),
            address = c.Address,
            gender = c.Gender,
            dateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            idCardNumber = c.IdCardNumber,
            passportExpiry = c.PassportExpiry?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            unitName = c.UnitName,
            taxCode = c.TaxCode,
            note = c.Note,
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

    /// <summary>Lưu (tạo/sửa) từ offcanvas — AJAX, trả JSON. Bám ĐÚNG luật của trang gốc.</summary>
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
