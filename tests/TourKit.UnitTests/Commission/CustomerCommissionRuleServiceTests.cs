using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Commission.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Commission;

public sealed class CustomerCommissionRuleServiceTests
{
    private static CustomerCommissionRuleService NewService(FakeRepository<CustomerCommissionRule>? repo = null)
        => new(
            repo ?? new FakeRepository<CustomerCommissionRule>(),
            new FakeRepository<CustomerType>(),
            new CreateCustomerCommissionRuleValidator(),
            new UpdateCustomerCommissionRuleValidator());

    [Fact]
    public async Task CreateAsync_rejects_percentage_over_100()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateAsync(new CreateCustomerCommissionRuleDto(1, 150m, 1)));
    }

    [Fact]
    public async Task Create_then_update_then_list_then_delete_roundtrip()
    {
        var repo = new FakeRepository<CustomerCommissionRule>();
        var service = NewService(repo);

        var created = await service.CreateAsync(new CreateCustomerCommissionRuleDto(2, 10m, 1));
        Assert.Equal(2, created.CustomerType);
        Assert.Equal(10m, created.Percentage);

        await service.UpdateAsync(created.Id, new UpdateCustomerCommissionRuleDto(15m, 0));

        var page = await service.ListAsync(1, 20);
        var one = Assert.Single(page.Items);
        Assert.Equal(15m, one.Percentage);
        Assert.Equal(0, one.Status);

        await service.DeleteAsync(created.Id);
        Assert.Empty((await service.ListAsync(1, 20)).Items);
    }

    [Fact]
    public async Task UpdateAsync_missing_throws_NotFound()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerCommissionRuleDto(5m, 1)));
    }
}
