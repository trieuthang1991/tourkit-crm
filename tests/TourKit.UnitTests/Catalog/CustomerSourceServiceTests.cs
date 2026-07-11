using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="CustomerSourceService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class CustomerSourceServiceTests
{
    private static CustomerSourceService NewService(out FakeRepository<CustomerSource> repo)
    {
        repo = new FakeRepository<CustomerSource>();
        return new CustomerSourceService(repo, new CreateCustomerSourceValidator(), new UpdateCustomerSourceValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCustomerSourceDto("Facebook", 1));

        Assert.Equal("Facebook", dto.Name);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerSourceDto("", 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCustomerSourceDto("Zalo", 1));

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerSourceDto("Zalo", 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCustomerSourceDto("Website", 1));

        await service.UpdateAsync(created.Id, new UpdateCustomerSourceDto("Google Ads", 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal("Google Ads", afterUpdate.Name);
        Assert.Equal(2, afterUpdate.SortOrder);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerSourceDto("X", 1)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
