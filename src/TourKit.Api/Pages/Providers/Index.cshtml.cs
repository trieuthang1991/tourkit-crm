using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Providers;

[Authorize(Policy = "provider.view")]
public class IndexModel : PageModel
{
    private readonly IProviderService _svc;
    private readonly IPaymentTermService _paymentTerms;
    private readonly IBranchService _branches;
    private readonly IMarketTypeService _marketTypes;

    public IndexModel(IProviderService svc, IPaymentTermService paymentTerms, IBranchService branches, IMarketTypeService marketTypes)
    {
        _svc = svc;
        _paymentTerms = paymentTerms;
        _branches = branches;
        _marketTypes = marketTypes;
    }

    public IReadOnlyList<ProviderDto> Items { get; private set; } = [];
    public IReadOnlyList<PaymentTermDto> PaymentTerms { get; private set; } = [];
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public IReadOnlyList<MarketTypeDto> MarketTypes { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public ProviderType Type { get; set; } = ProviderType.Hotel;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public Guid? PaymentTermId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? MarketTypeId { get; set; }
        public int Rate { get; set; }
        public int Status { get; set; } = 1;
    }

    public static string TypeLabel(ProviderType t) => t switch
    {
        ProviderType.Hotel => "Khách sạn", ProviderType.Vehicle => "Vận chuyển", ProviderType.Restaurant => "Nhà hàng",
        ProviderType.Guide => "Hướng dẫn viên", ProviderType.Airline => "Hàng không", ProviderType.Other => "Khác", _ => t.ToString(),
    };

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        PaymentTerms = await _paymentTerms.ListAsync();
        Branches = await _branches.ListAsync();
        MarketTypes = await _marketTypes.ListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateProviderDto(
                Input.Name, Input.Type, Input.Phone, Input.Email, Input.Address, Input.TaxCode, Input.ContactPerson,
                Input.BankAccount, Input.BankName, Input.PaymentTermId, Input.Rate, Input.Status,
                Province: null, BranchId: Input.BranchId, MarketTypeId: Input.MarketTypeId));
        }
        else
        {
            await _svc.CreateAsync(new CreateProviderDto(
                Input.Code, Input.Name, Input.Type, Input.Phone, Input.Email, Input.Address, Input.TaxCode, Input.ContactPerson,
                Input.BankAccount, Input.BankName, Input.PaymentTermId, Input.Rate, Input.Status,
                Province: null, BranchId: Input.BranchId, MarketTypeId: Input.MarketTypeId));
        }

        return new JsonResult(Result.Success("Đã lưu nhà cung cấp."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá nhà cung cấp.";
        return RedirectToPage();
    }
}
