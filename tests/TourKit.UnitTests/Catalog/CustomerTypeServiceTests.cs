using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="CustomerTypeService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class CustomerTypeServiceTests
{
    private static CustomerTypeService NewService(out FakeRepository<CustomerType> repo)
    {
        repo = new FakeRepository<CustomerType>();
        return new CustomerTypeService(repo, new CreateCustomerTypeValidator(), new UpdateCustomerTypeValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCustomerTypeDto(1, "Khách lẻ", 1));

        Assert.Equal(1, dto.Code);
        Assert.Equal("Khách lẻ", dto.Name);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerTypeDto(1, "", 1)));
    }

    [Fact]
    public async Task CreateAsync_non_positive_code_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerTypeDto(0, "Khách lẻ", 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCustomerTypeDto(1, "Khách lẻ", 1));

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerTypeDto(1, "Khách đoàn", 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCustomerTypeDto(1, "Khách lẻ", 1));

        await service.UpdateAsync(created.Id, new UpdateCustomerTypeDto(2, "Khách đoàn", 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal(2, afterUpdate.Code);
        Assert.Equal("Khách đoàn", afterUpdate.Name);
        Assert.Equal(2, afterUpdate.SortOrder);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_keeping_own_code_is_allowed()
    {
        var service = NewService(out _);
        var created = await service.CreateAsync(new CreateCustomerTypeDto(5, "VIP", 1));

        // Giữ nguyên Code của chính nó (không coi là trùng).
        await service.UpdateAsync(created.Id, new UpdateCustomerTypeDto(5, "VIP+", 1));
        Assert.Equal("VIP+", (await service.ListAsync()).Single().Name);
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerTypeDto(1, "X", 1)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
