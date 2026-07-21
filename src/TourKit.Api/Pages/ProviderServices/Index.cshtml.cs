using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Pages.ProviderServices;

// Bảng giá NCC: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/services/ProviderServicesPage.tsx): cột NCC, tên gói giá, giá hợp đồng (nguyên tệ
// + quy đổi VND), giá công bố, tiền tệ, trạng thái; lọc theo NCC (tiêu chí DUY NHẤT service hỗ trợ).
[Authorize(Policy = "service.view")]
public class IndexModel : TkListPageModel
{
    private readonly IProviderServiceService _svc;
    private readonly IProviderService _providers;
    private readonly IServiceItemService _items;
    private readonly ICurrencyService _currencies;

    public IndexModel(IProviderServiceService svc, IProviderService providers, IServiceItemService items, ICurrencyService currencies)
    {
        _svc = svc;
        _providers = providers;
        _items = items;
        _currencies = currencies;
    }

    /// <summary>Danh mục dịch vụ cho offcanvas — nạp có giới hạn (danh mục nhỏ), không get-all.</summary>
    public IReadOnlyList<ServiceItemDto> ServiceItems { get; private set; } = [];
    public IReadOnlyList<CurrencyDto> Currencies { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc chọn NCC")] public Guid ProviderId { get; set; }
        public Guid? ServiceItemId { get; set; }
        public string? PriceName { get; set; }
        public decimal ContractPrice { get; set; }
        public decimal PublicPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public int AmountOfPeople { get; set; } = 1;
        public string? Note { get; set; }
        public int Status { get; set; } = 1;
    }

    public static string StatusLabel(int s) => s == 1 ? "Hoạt động" : "Ngừng";

    private const int LookupSize = 200;

    public async Task OnGetAsync()
    {
        ServiceItems = (await _items.ListAsync(1, LookupSize)).Items;
        Currencies = await _currencies.ListAsync();
    }

    /// <summary>Nguồn Select2 ajax cho NCC (danh sách lớn): lọc keyword ở SERVER.</summary>
    public async Task<IActionResult> OnGetProviderOptionsAsync()
    {
        var q = Request.Query["q"].ToString();
        var keyword = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var result = await _providers.ListAsync(1, 30, new ProviderListFilter(Q: keyword));
        return new JsonResult(new
        {
            results = result.Items.Select(p => new { id = p.Id, text = $"{p.Name} ({p.Code})" }),
        });
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + đủ field cho offcanvas sửa.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var providerId = Guid.TryParse(Request.Query["providerId"], out var pid) ? pid : (Guid?)null;
        var result = await _svc.ListAsync(dt.Page, dt.Size, providerId, dt.Keyword);

        // Tên NCC: chỉ tra cho NCC xuất hiện trong TRANG hiện tại (không nạp toàn bảng).
        var providerNames = new Dictionary<Guid, string>();
        foreach (var id in result.Items.Select(x => x.ProviderId).Distinct())
        {
            try
            {
                var p = await _providers.GetAsync(id);
                providerNames[id] = p.Name;
            }
            catch (NotFoundException)
            {
                providerNames[id] = "—";
            }
        }

        var itemNames = (await _items.ListAsync(1, LookupSize)).Items.ToDictionary(s => s.Id, s => s.Name);

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            providerId = x.ProviderId,
            providerName = providerNames.GetValueOrDefault(x.ProviderId, "—"),
            serviceItemId = x.ServiceItemId,
            serviceItemName = x.ServiceItemId is Guid sid ? itemNames.GetValueOrDefault(sid, "") : "",
            priceName = x.PriceName,
            contractPrice = x.ContractPrice,
            publicPrice = x.PublicPrice,
            contractPriceVnd = x.ContractPriceVnd,
            publicPriceVnd = x.PublicPriceVnd,
            currencyCode = x.CurrencyCode ?? "VND",
            amountOfPeople = x.AmountOfPeople,
            note = x.Note,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI — quy đổi VND (giá hợp đồng · giá công bố).
        var pageSum = new
        {
            contractPriceVnd = data.Sum(x => x.contractPriceVnd),
            publicPriceVnd = data.Sum(x => x.publicPriceVnd),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = result.Total,
            recordsFiltered = result.Total,
            data,
            pageSum,
        });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateProviderServiceDto(
                Input.ServiceItemId, Input.PriceName, Input.ContractPrice, Input.PublicPrice,
                Input.CurrencyCode, Input.AmountOfPeople, Input.Note, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateProviderServiceDto(
                Input.ProviderId, Input.ServiceItemId, Input.PriceName, Input.ContractPrice, Input.PublicPrice,
                Input.CurrencyCode, Input.AmountOfPeople, Input.Note, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu bảng giá NCC."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá bảng giá NCC.";
        return RedirectToPage();
    }
}
