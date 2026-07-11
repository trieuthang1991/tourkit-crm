using TourKit.Application.Common;
using TourKit.Application.Operations;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Operations;

/// <summary>Test <see cref="GuideTransactionService"/> — thu-chi HDV + đối soát net.</summary>
public class GuideTransactionServiceTests
{
    private static GuideTransactionService NewService(
        out FakeRepository<GuideTransaction> repo, out FakeRepository<TourGuideAssignment> assignRepo)
    {
        repo = new FakeRepository<GuideTransaction>();
        assignRepo = new FakeRepository<TourGuideAssignment>();
        return new GuideTransactionService(repo, assignRepo);
    }

    private static async Task<TourGuideAssignment> SeedAssignmentAsync(FakeRepository<TourGuideAssignment> repo)
    {
        var a = new TourGuideAssignment { TourDepartureId = Guid.NewGuid(), ProviderId = Guid.NewGuid() };
        await repo.AddAsync(a);
        await repo.SaveChangesAsync();
        return a;
    }

    [Fact]
    public async Task Create_then_summary_computes_net()
    {
        var service = NewService(out _, out var assignRepo);
        var a = await SeedAssignmentAsync(assignRepo);

        await service.CreateAsync(a.Id, new CreateGuideTransactionDto((int)GuideTransactionType.Revenue, 2_000_000m, "Bán thêm vé show", null));
        await service.CreateAsync(a.Id, new CreateGuideTransactionDto((int)GuideTransactionType.Expense, 500_000m, "Vé vào cửa", null));
        await service.CreateAsync(a.Id, new CreateGuideTransactionDto((int)GuideTransactionType.Expense, 300_000m, "Tip tài xế", null));

        var settlement = await service.GetByAssignmentAsync(a.Id);

        Assert.Equal(2_000_000m, settlement.TotalRevenue);
        Assert.Equal(800_000m, settlement.TotalExpense);
        Assert.Equal(1_200_000m, settlement.Net);
        Assert.Equal(3, settlement.Items.Length);
    }

    [Fact]
    public async Task Create_unknown_assignment_throws_NotFound()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreateGuideTransactionDto(0, 1m, "x", null)));
    }

    [Fact]
    public async Task Create_non_positive_amount_throws()
    {
        var service = NewService(out _, out var assignRepo);
        var a = await SeedAssignmentAsync(assignRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            a.Id, new CreateGuideTransactionDto(0, 0m, "x", null)));
    }

    [Fact]
    public async Task Delete_removes_and_updates_net()
    {
        var service = NewService(out _, out var assignRepo);
        var a = await SeedAssignmentAsync(assignRepo);
        var rev = await service.CreateAsync(a.Id, new CreateGuideTransactionDto(0, 1_000_000m, "Thu", null));

        await service.DeleteAsync(a.Id, rev.Id);

        var settlement = await service.GetByAssignmentAsync(a.Id);
        Assert.Equal(0m, settlement.Net);
        Assert.Empty(settlement.Items);
    }

    [Fact]
    public async Task Delete_of_other_assignment_throws_NotFound()
    {
        var service = NewService(out _, out var assignRepo);
        var a = await SeedAssignmentAsync(assignRepo);
        var rev = await service.CreateAsync(a.Id, new CreateGuideTransactionDto(0, 1m, "x", null));

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid(), rev.Id));
    }
}
