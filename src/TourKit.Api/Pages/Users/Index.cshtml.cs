using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.Users;

[Authorize(Policy = "user.view")]
public class IndexModel : PageModel
{
    private readonly IUserAdminService _users;
    private readonly IDepartmentService _depts;
    private readonly IPositionService _positions;
    private readonly IRoleAdminService _roles;
    private readonly IPasswordHasher _hasher;
    public IndexModel(IUserAdminService users, IDepartmentService depts, IPositionService positions, IRoleAdminService roles, IPasswordHasher hasher)
    {
        _users = users; _depts = depts; _positions = positions; _roles = roles; _hasher = hasher;
    }

    public IReadOnlyList<UserListDto> Items { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> Departments { get; private set; } = [];
    public IReadOnlyList<PositionDto> Positions { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Roles { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập email")] public string Email { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập họ tên")] public string FullName { get; set; } = "";
        public string? Password { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public Guid? RoleId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        Items = await _users.ListAsync();
        Departments = await _depts.ListAsync();
        Positions = await _positions.ListAsync();
        Roles = (await _roles.ListRolesAsync()).Select(r => (r.Id, r.Name)).ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _users.UpdateAsync(g, new UpdateUserRequest(Input.FullName, Input.DepartmentId, Input.PositionId, Input.RoleId, Input.IsActive));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Input.Password))
            {
                return new JsonResult(Result.Error("Bắt buộc nhập mật khẩu khi tạo mới."));
            }

            await _users.CreateAsync(new CreateUserData(Input.Email, Input.FullName, _hasher.Hash(Input.Password),
                Input.DepartmentId, Input.PositionId, Input.RoleId, Input.IsActive));
        }

        return new JsonResult(Result.Success("Đã lưu người dùng."));
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        await _users.ToggleActiveAsync(id);
        TempData["ok"] = "Đã đổi trạng thái người dùng.";
        return RedirectToPage();
    }

    /// <summary>Quản trị đặt lại mật khẩu hộ (khác luồng tự quên mật khẩu qua email ở /Auth/ForgotPassword).</summary>
    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return new JsonResult(Result.Error("Mật khẩu mới tối thiểu 8 ký tự."));
        }

        await _users.ResetPasswordAsync(id, _hasher.Hash(newPassword));
        return new JsonResult(Result.Success("Đã đặt lại mật khẩu."));
    }
}
