using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="CustomerTagService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class CustomerTagServiceTests
{
    private static CustomerTagService NewService(out FakeRepository<CustomerTag> repo)
    {
        repo = new FakeRepository<CustomerTag>();
        return new CustomerTagService(repo, new CreateCustomerTagValidator(), new UpdateCustomerTagValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_with_color_and_persists()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCustomerTagDto("VIP", "gold", 1));

        Assert.Equal("VIP", dto.Name);
        Assert.Equal("gold", dto.Color);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerTagDto("", null, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCustomerTagDto("Thân thiết", "red", 1));

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerTagDto("Thân thiết", "blue", 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCustomerTagDto("Mới", null, 1));

        await service.UpdateAsync(created.Id, new UpdateCustomerTagDto("Tiềm năng", "green", 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal("Tiềm năng", afterUpdate.Name);
        Assert.Equal("green", afterUpdate.Color);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerTagDto("X", null, 1)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
