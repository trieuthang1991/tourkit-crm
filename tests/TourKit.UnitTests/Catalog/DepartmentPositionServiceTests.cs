using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="DepartmentService"/> + <see cref="PositionService"/> (catalog cơ cấu tổ chức).</summary>
public class DepartmentPositionServiceTests
{
    private static DepartmentService NewDepartments(out FakeRepository<Department> repo)
    {
        repo = new FakeRepository<Department>();
        return new DepartmentService(repo, new CreateDepartmentValidator(), new UpdateDepartmentValidator());
    }

    private static PositionService NewPositions(out FakeRepository<Position> repo)
    {
        repo = new FakeRepository<Position>();
        return new PositionService(repo, new CreatePositionValidator(), new UpdatePositionValidator());
    }

    [Fact]
    public async Task Department_create_update_delete_roundtrip()
    {
        var service = NewDepartments(out var repo);
        var created = await service.CreateAsync(new CreateDepartmentDto("Điều hành", "DH", 1));
        Assert.Equal("Điều hành", created.Name);
        Assert.Equal("DH", created.Code);

        await service.UpdateAsync(created.Id, new UpdateDepartmentDto("Kinh doanh", "KD", 2));
        Assert.Equal("Kinh doanh", (await service.ListAsync()).Single().Name);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task Department_duplicate_name_throws()
    {
        var service = NewDepartments(out _);
        await service.CreateAsync(new CreateDepartmentDto("Kế toán", null, 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateDepartmentDto("Kế toán", null, 2)));
    }

    [Fact]
    public async Task Department_empty_name_throws()
    {
        var service = NewDepartments(out _);
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateDepartmentDto("", null, 1)));
    }

    [Fact]
    public async Task Position_create_and_duplicate_and_notfound()
    {
        var service = NewPositions(out _);
        var created = await service.CreateAsync(new CreatePositionDto("Trưởng phòng", 1));
        Assert.Equal("Trưởng phòng", created.Name);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreatePositionDto("Trưởng phòng", 2)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new UpdatePositionDto("X", 1)));
    }
}
