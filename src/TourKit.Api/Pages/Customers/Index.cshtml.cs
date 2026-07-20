using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerService _service;
    private readonly ICustomerTypeService _types;
    private readonly ICustomerSourceService _sources;
    private readonly ICustomerTagService _tags;
    private readonly IMarketTypeService _markets;
    public IndexModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _tags = tags;
        _markets = markets;
    }

    // Thẻ thống kê + facet cho lần render đầu (DataTables lo bảng/search/paging qua OnGetData).
    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } =
        new([], [], [], [], [], [], [], [], [], []);

    // Bind cho offcanvas Save (create/update dùng chung; Id rỗng = tạo mới).
    [BindProperty] public CustomerFormInput Input { get; set; } = new();
    [BindProperty] public Guid? Id { get; set; }

    public async Task OnGetAsync()
    {
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
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

    /// <summary>Nguồn dữ liệu DataTables (server-side processing) — kèm đủ field để offcanvas sửa điền lại.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var q = Request.Query;
        var start = ParseInt(q["start"], 0);
        var length = ParseInt(q["length"], 20);
        if (length <= 0) { length = 20; }

        var search = q["search[value]"].ToString();
        var source = q["source"].ToString();
        var city = q["city"].ToString();

        var filter = new CustomerListFilter(
            Q: string.IsNullOrWhiteSpace(search) ? null : search,
            Source: string.IsNullOrWhiteSpace(source) ? null : source,
            City: string.IsNullOrWhiteSpace(city) ? null : city);

        var page = (start / length) + 1;
        var result = await _service.ListAsync(page, length, filter);
        var stats = await _service.GetStatsAsync();

        var data = result.Items.Select(c => new
        {
            id = c.Id,
            code = c.Code,
            fullName = c.FullName,
            phone = c.Phone,
            email = c.Email,
            customerType = c.CustomerType,
            source = c.Source,
            city = c.City,
            address = c.Address,
            tags = c.Tags.Count > 0 ? c.Tags : (string.IsNullOrWhiteSpace(c.Tag) ? Array.Empty<string>() : new[] { c.Tag }),
            tag = c.Tags.Count > 0 ? string.Join(", ", c.Tags) : c.Tag,   // hiển thị ở lưới
            gender = c.Gender,
            dateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            idCardNumber = c.IdCardNumber,
            passportExpiry = c.PassportExpiry?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            marketGroup = c.MarketGroup,
            collaboratorName = c.CollaboratorName,
            unitName = c.UnitName,
            taxCode = c.TaxCode,
            note = c.Note,
            revenue = c.Revenue,
            createdAt = c.CreatedAt.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        });

        return new JsonResult(new
        {
            draw = ParseInt(q["draw"], 0),
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
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
                DateOfBirth: Input.DateOfBirth, IdCardNumber: Input.IdCardNumber, PassportExpiry: Input.PassportExpiry,
                Gender: Input.Gender, City: Input.City, MarketGroup: Input.MarketGroup,
                CollaboratorName: Input.CollaboratorName,
                UnitName: Input.UnitName, TaxCode: Input.TaxCode, Note: Input.Note));
        }
        else
        {
            await _service.CreateAsync(new CreateCustomerDto(
                FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
                Source: Input.Source, Tag: Input.Tags.FirstOrDefault(), Tags: Input.Tags, Email: Input.Email, Address: Input.Address,
                DateOfBirth: Input.DateOfBirth, IdCardNumber: Input.IdCardNumber, PassportExpiry: Input.PassportExpiry,
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

    private static int ParseInt(Microsoft.Extensions.Primitives.StringValues v, int fallback) =>
        int.TryParse(v.ToString(), out var n) ? n : fallback;
}
