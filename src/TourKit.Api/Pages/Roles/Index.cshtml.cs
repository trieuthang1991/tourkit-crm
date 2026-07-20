using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Admin;

namespace TourKit.Api.Pages.Roles;

[Authorize(Policy = "user.view")]
public class IndexModel : PageModel
{
    private readonly IRoleAdminService _roles;
    public IndexModel(IRoleAdminService roles) => _roles = roles;

    public IReadOnlyList<RoleRow> Items { get; private set; } = [];
    public IReadOnlyList<IGrouping<string, PermissionDto>> PermGroups { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed record RoleRow(Guid Id, string Name, int PermissionCount, int UserCount, IReadOnlyList<Guid> PermissionIds);

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên vai trò")] public string Name { get; set; } = "";
        public List<Guid> PermissionIds { get; set; } = [];
    }

    public async Task OnGetAsync()
    {
        var perms = await _roles.ListPermissionsAsync();
        PermGroups = perms.GroupBy(p => string.IsNullOrWhiteSpace(p.Group) ? "Khác" : p.Group).ToList();

        var list = await _roles.ListRolesAsync();
        var rows = new List<RoleRow>(list.Count);
        foreach (var r in list)
        {
            var detail = await _roles.GetRoleAsync(r.Id);
            rows.Add(new RoleRow(r.Id, r.Name, r.PermissionCount, r.UserCount, detail.PermissionIds));
        }
        Items = rows;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _roles.UpdateAsync(g, new UpdateRoleRequest(Input.Name, Input.PermissionIds));
            }
            else
            {
                var created = await _roles.CreateAsync(new CreateRoleRequest(Input.Name));
                await _roles.UpdateAsync(created.Id, new UpdateRoleRequest(Input.Name, Input.PermissionIds));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu vai trò."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _roles.DeleteAsync(id);
            TempData["ok"] = "Đã xoá vai trò.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage();
    }
}
