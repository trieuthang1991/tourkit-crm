using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Finance.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Finance;

public sealed class TicketFundServiceTests
{
    private static TicketFundService NewService(FakeRepository<TicketFund>? repo = null)
        => new(repo ?? new FakeRepository<TicketFund>(), new CreateTicketFundValidator());

    [Fact]
    public async Task CreateAsync_rejects_empty_order()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateAsync(new CreateTicketFundDto(Guid.Empty, null, null, "T1", 0, false)));
    }

    [Fact]
    public async Task Create_then_update_then_list_filter_then_delete_roundtrip()
    {
        var repo = new FakeRepository<TicketFund>();
        var service = NewService(repo);
        var orderId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateTicketFundDto(orderId, null, null, "VE-001", 1, false));
        Assert.Equal(orderId, created.OrderId);
        Assert.Equal("VE-001", created.TicketCode);

        await service.UpdateAsync(created.Id, new UpdateTicketFundDto(null, null, "VE-002", 2, true));

        var byOrder = await service.ListAsync(1, 20, orderId);
        var one = Assert.Single(byOrder.Items);
        Assert.Equal("VE-002", one.TicketCode);
        Assert.True(one.IsClosed);

        var otherOrder = await service.ListAsync(1, 20, Guid.NewGuid());
        Assert.Empty(otherOrder.Items);

        await service.DeleteAsync(created.Id);
        Assert.Empty((await service.ListAsync(1, 20, null)).Items);
    }

    [Fact]
    public async Task UpdateAsync_missing_throws_NotFound()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateTicketFundDto(null, null, "x", 0, false)));
    }
}
