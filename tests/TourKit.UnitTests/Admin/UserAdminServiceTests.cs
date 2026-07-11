using TourKit.Application.Admin;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Admin;

/// <summary>Test <see cref="UserAdminService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class UserAdminServiceTests
{
    private static UserAdminService NewService(
        out FakeRepository<User> users, out FakeRepository<Department> departments, out FakeRepository<Position> positions)
    {
        users = new FakeRepository<User>();
        departments = new FakeRepository<Department>();
        positions = new FakeRepository<Position>();
        return new UserAdminService(users, departments, positions);
    }

    [Fact]
    public async Task AssignOrgAsync_sets_ids_and_resolves_names()
    {
        var service = NewService(out var users, out var departments, out var positions);
        var user = new User { Email = "a@b.c", FullName = "Nguyễn Văn A" };
        var dept = new Department { Name = "Điều hành" };
        var pos = new Position { Name = "Trưởng phòng" };
        await users.AddAsync(user);
        await departments.AddAsync(dept);
        await positions.AddAsync(pos);
        await users.SaveChangesAsync();
        await departments.SaveChangesAsync();
        await positions.SaveChangesAsync();

        var result = await service.AssignOrgAsync(user.Id, new AssignUserOrgDto(dept.Id, pos.Id));

        Assert.Equal(dept.Id, result.DepartmentId);
        Assert.Equal("Điều hành", result.DepartmentName);
        Assert.Equal("Trưởng phòng", result.PositionName);

        var stored = await users.GetByIdAsync(user.Id);
        Assert.Equal(dept.Id, stored!.DepartmentId);
    }

    [Fact]
    public async Task AssignOrgAsync_null_clears_assignment()
    {
        var service = NewService(out var users, out _, out _);
        var user = new User { Email = "a@b.c", FullName = "A", DepartmentId = Guid.NewGuid() };
        await users.AddAsync(user);
        await users.SaveChangesAsync();

        var result = await service.AssignOrgAsync(user.Id, new AssignUserOrgDto(null, null));

        Assert.Null(result.DepartmentId);
        Assert.Null(result.PositionName);
    }

    [Fact]
    public async Task AssignOrgAsync_unknown_department_throws_ValidationAppException()
    {
        var service = NewService(out var users, out _, out _);
        var user = new User { Email = "a@b.c", FullName = "A" };
        await users.AddAsync(user);
        await users.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.AssignOrgAsync(user.Id, new AssignUserOrgDto(Guid.NewGuid(), null)));
    }

    [Fact]
    public async Task AssignOrgAsync_unknown_user_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignOrgAsync(Guid.NewGuid(), new AssignUserOrgDto(null, null)));
    }

    [Fact]
    public async Task ListAsync_returns_users_with_resolved_org_names()
    {
        var service = NewService(out var users, out var departments, out _);
        var dept = new Department { Name = "Kế toán" };
        await departments.AddAsync(dept);
        await departments.SaveChangesAsync();
        await users.AddAsync(new User { Email = "x@y.z", FullName = "B", DepartmentId = dept.Id });
        await users.SaveChangesAsync();

        var list = await service.ListAsync();

        Assert.Single(list);
        Assert.Equal("Kế toán", list[0].DepartmentName);
    }
}
