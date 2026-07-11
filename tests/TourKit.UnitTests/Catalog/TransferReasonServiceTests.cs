using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="TransferReasonService"/> — danh mục lý do chuyển chuyến (legacy ReasonSwitch).</summary>
public class TransferReasonServiceTests
{
    private static TransferReasonService NewService(out FakeRepository<TransferReason> repo)
    {
        repo = new FakeRepository<TransferReason>();
        return new TransferReasonService(repo, new CreateTransferReasonValidator(), new UpdateTransferReasonValidator());
    }

    [Fact]
    public async Task CreateAsync_persists_and_orders_by_sort()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateTransferReasonDto("Khách kẹt lịch", 2));
        await service.CreateAsync(new CreateTransferReasonDto("Gộp đoàn", 1));

        var list = await service.ListAsync();
        Assert.Equal("Gộp đoàn", list[0].Name);
        Assert.Equal("Khách kẹt lịch", list[1].Name);
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateTransferReasonDto(" ", 0)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateTransferReasonDto("Gộp đoàn", 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateTransferReasonDto("Gộp đoàn", 2)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
