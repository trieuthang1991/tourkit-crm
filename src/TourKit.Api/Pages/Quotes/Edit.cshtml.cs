using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Quotes;

[Authorize(Policy = "quote.view")]
public class EditModel : PageModel
{
    private readonly IQuoteService _svc;
    private readonly ICustomerService _customers;
    public EditModel(IQuoteService svc, ICustomerService customers)
    {
        _svc = svc;
        _customers = customers;
    }

    private static readonly JsonSerializerOptions LinesJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public QuoteDto? Quote { get; private set; }

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public string LinesJson { get; set; } = "[]";

    public sealed class InputModel
    {
        public string Code { get; set; } = "";
        public Guid? CustomerId { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập tên khách")] public string CustomerName { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        public DateTimeOffset? ValidUntil { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public decimal ChildPercent { get; set; } = 75;
        public decimal InfantPercent { get; set; } = 50;
        public int QuoteType { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id is Guid g && g != Guid.Empty)
        {
            try
            {
                Quote = await _svc.GetAsync(g);
            }
            catch (Exception)
            {
                return NotFound();
            }
            Id = Quote.Id;
        }
        return Page();
    }

    public async Task<IActionResult> OnGetCustomerSearchAsync(string? q)
    {
        var result = await _customers.ListAsync(1, 20, new CustomerListFilter(Q: q));
        var items = result.Items.Select(c => new { id = c.Id, text = $"{c.FullName}{(string.IsNullOrEmpty(c.Phone) ? "" : " - " + c.Phone)}" });
        return new JsonResult(new { results = items });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        CreateQuoteLineDto[] lines;
        try
        {
            lines = JsonSerializer.Deserialize<CreateQuoteLineDto[]>(LinesJson, LinesJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return new JsonResult(Result.Error("Danh sách chi phí không hợp lệ."));
        }

        if (lines.Length == 0)
        {
            return new JsonResult(Result.Error("Cần ít nhất một dòng chi phí/dịch vụ."));
        }

        var code = string.IsNullOrWhiteSpace(Input.Code) ? $"BG{DateTimeOffset.UtcNow:yyMMddHHmmss}" : Input.Code.Trim();
        var validUntil = Input.ValidUntil?.ToUniversalTime();

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                var updated = await _svc.UpdateAsync(g, new UpdateQuoteDto(
                    code, Input.CustomerId, Input.CustomerName, Input.Title, validUntil, Input.Status, Input.Note,
                    lines, Input.Adults, Input.Children, Input.Infants, Input.ChildPercent, Input.InfantPercent, Input.QuoteType));
                return new JsonResult(Result.Success("Đã lưu báo giá.", new { id = updated.Id }));
            }

            var created = await _svc.CreateAsync(new CreateQuoteDto(
                code, Input.CustomerId, Input.CustomerName, Input.Title, validUntil, Input.Status, Input.Note,
                lines, Input.Adults, Input.Children, Input.Infants, Input.ChildPercent, Input.InfantPercent, Input.QuoteType));
            return new JsonResult(Result.Success("Đã tạo báo giá.", new { id = created.Id }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
