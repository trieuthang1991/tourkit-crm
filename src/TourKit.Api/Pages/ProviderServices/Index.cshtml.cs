using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Pages.ProviderServices;

[Authorize(Policy = "service.view")]
public class IndexModel : PageModel
{
    private readonly IProviderServiceService _svc;
    private readonly IProviderService _providers;
    private readonly IServiceItemService _items;

    public IndexModel(IProviderServiceService svc, IProviderService providers, IServiceItemService items)
    {
        _svc = svc;
        _providers = providers;
        _items = items;
    }

    public IReadOnlyList<ProviderServiceDto> Items { get; private set; } = [];
    public IReadOnlyList<ProviderDto> Providers { get; private set; } = [];
    public IReadOnlyList<ServiceItemDto> ServiceItems { get; private set; } = [];
    private Dictionary<Guid, string> _providerNames = new();

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

    public string ProviderName(Guid id) => _providerNames.TryGetValue(id, out var n) ? n : "—";

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000, null)).Items;
        Providers = (await _providers.ListAsync(1, 1000)).Items;
        ServiceItems = (await _items.ListAsync(1, 1000)).Items;
        _providerNames = Providers.ToDictionary(p => p.Id, p => p.Name);
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
