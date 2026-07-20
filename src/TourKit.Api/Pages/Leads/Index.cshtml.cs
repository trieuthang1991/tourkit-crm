using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Leads;

[Authorize(Policy = "lead.view")]
public class IndexModel : PageModel
{
    private readonly ILeadService _svc;
    private readonly IUserAdminService _users;
    private readonly IBranchService _branches;
    public IndexModel(ILeadService svc, IUserAdminService users, IBranchService branches)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
    }

    public IReadOnlyList<LeadDto> Items { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    private Dictionary<Guid, string> _userNames = new();

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập họ tên")] public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public Guid? AssignedToUserId { get; set; }
        public Guid? BranchId { get; set; }
    }

    public string UserName(Guid? id) => id is { } g && _userNames.TryGetValue(g, out var n) ? n : "—";

    public static string StatusLabel(LeadStatus s) => s switch
    {
        LeadStatus.New => "Mới", LeadStatus.Contacted => "Đã liên hệ", LeadStatus.Qualified => "Tiềm năng",
        LeadStatus.Won => "Đã chốt", LeadStatus.Lost => "Thất bại", _ => s.ToString(),
    };

    public static string StatusColor(LeadStatus s) => s switch
    {
        LeadStatus.Won => "success", LeadStatus.Lost => "danger", LeadStatus.Qualified => "info",
        LeadStatus.Contacted => "warning", _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 1000)).Items;
        var users = await _users.ListAsync();
        Users = users.Select(u => (u.Id, u.FullName)).ToList();
        _userNames = users.ToDictionary(u => u.Id, u => u.FullName);
        Branches = await _branches.ListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateLeadDto(Input.FullName, Input.Phone, Input.Email, Input.Source, Input.Status, Input.AssignedToUserId, Input.BranchId));
        }
        else
        {
            await _svc.CreateAsync(new CreateLeadDto(Input.FullName, Input.Phone, Input.Email, Input.Source, Input.AssignedToUserId, Input.BranchId));
        }

        return new JsonResult(Result.Success("Đã lưu cơ hội bán hàng."));
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid id)
    {
        await _svc.ConvertAsync(id);
        TempData["ok"] = "Đã chuyển lead thành khách hàng.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá cơ hội.";
        return RedirectToPage();
    }
}
