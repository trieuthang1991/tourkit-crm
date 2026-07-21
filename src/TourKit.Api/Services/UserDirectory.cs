using TourKit.Infrastructure.Tenancy;
using TourKit.Application.Admin;
using TourKit.Caching;

namespace TourKit.Api.Services;

/// <summary>
/// Danh bạ người dùng có CACHE ngắn, dùng chung cho mọi màn cần đổ combobox "nhân viên" hoặc
/// tra tên từ Id.
///
/// Vì sao cần: danh sách nhân viên gần như không đổi, nhưng trước đây mỗi lần bảng DataTables lật
/// trang là gọi lại <c>IUserAdminService.ListAsync()</c> — nạp TOÀN BỘ bảng Users chỉ để tra vài
/// cái tên. Màn Cơ hội gọi ở 3 handler khác nhau, màn Đơn hàng/Chuyến đi gọi trong chính handler
/// phân trang. Cache 60 giây cắt hẳn nhóm truy vấn lặp này.
///
/// Đánh đổi: thêm/sửa nhân viên có thể chậm hiện tối đa 60 giây ở các combobox — nên màn Quản lý
/// người dùng gọi <see cref="InvalidateAsync"/> ngay sau khi ghi. Khoá cache tách theo tenant để
/// không rò dữ liệu giữa các đơn vị.
/// </summary>
public sealed class UserDirectory
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly IUserAdminService _users;
    private readonly ITkCache _cache;
    private readonly AmbientTenantContext _tenant;

    public UserDirectory(IUserAdminService users, ITkCache cache, AmbientTenantContext tenant)
    {
        _users = users;
        _cache = cache;
        _tenant = tenant;
    }

    private string Key => $"userdir:{_tenant.TenantId}";

    public Task<IReadOnlyList<UserListDto>> ListAsync()
        => _cache.GetOrSetAsync<IReadOnlyList<UserListDto>>(Key, Ttl, async () => await _users.ListAsync());

    /// <summary>Bảng tra Id → tên, cho các handler cần đổi Id thành tên hiển thị.</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync()
    {
        var list = await ListAsync();
        return list.ToDictionary(u => u.Id, u => u.FullName);
    }

    /// <summary>Xoá cache ngay sau khi thêm/sửa/khoá nhân viên để combobox không hiện dữ liệu cũ.</summary>
    public Task InvalidateAsync() => _cache.RemoveAsync(Key);
}
