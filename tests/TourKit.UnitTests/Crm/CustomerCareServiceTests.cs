using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Crm.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test <see cref="CustomerCareService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class CustomerCareServiceTests
{
    private static CustomerCareService NewService(out FakeRepository<CustomerCare> repo, out FakeRepository<Customer> customerRepo)
    {
        repo = new FakeRepository<CustomerCare>();
        customerRepo = new FakeRepository<Customer>();
        return new CustomerCareService(repo, customerRepo, new FakeRepository<User>(), new CreateCustomerCareValidator(), new UpdateCustomerCareValidator());
    }

    private static async Task<Customer> SeedCustomerAsync(FakeRepository<Customer> customerRepo)
    {
        var customer = new Customer { FullName = "Nguyễn Văn A" };
        await customerRepo.AddAsync(customer);
        await customerRepo.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out var customerRepo);
        var customer = await SeedCustomerAsync(customerRepo);

        var dto = await service.CreateAsync(new CreateCustomerCareDto(customer.Id, "Gọi nhắc lịch", "Chi tiết", null, null, 0));

        Assert.Equal("Gọi nhắc lịch", dto.Title);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal(customer.Id, stored!.CustomerId);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_title_throws_ValidationAppException()
    {
        var service = NewService(out _, out var customerRepo);
        var customer = await SeedCustomerAsync(customerRepo);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerCareDto(customer.Id, "", null, null, null, 0)));
    }

    [Fact]
    public async Task CreateAsync_unknown_customer_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCustomerCareDto(Guid.NewGuid(), "Gọi nhắc lịch", null, null, null, 0)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(), new UpdateCustomerCareDto("Đã gọi", null, null, "Khách hài lòng", null, 1)));
    }
}
