using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="SurchargeService"/> (danh mục loại phụ thu).</summary>
public class SurchargeServiceTests
{
    private static SurchargeService NewService(out FakeRepository<Surcharge> repo)
    {
        repo = new FakeRepository<Surcharge>();
        return new SurchargeService(repo, new CreateSurchargeValidator(), new UpdateSurchargeValidator());
    }

    [Fact]
    public async Task CreateAsync_persists_calc_type_and_value()
    {
        var service = NewService(out _);

        var dto = await service.CreateAsync(new CreateSurchargeDto("Cao điểm", (int)SurchargeCalcType.Percent, 10m, 1));

        Assert.Equal((int)SurchargeCalcType.Percent, dto.CalcType);
        Assert.Equal(10m, dto.DefaultValue);
    }

    [Fact]
    public async Task CreateAsync_invalid_calc_type_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateSurchargeDto("X", 5, 1m, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateSurchargeDto("Phòng đơn", 0, 500_000m, 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateSurchargeDto("Phòng đơn", 0, 600_000m, 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateSurchargeDto("Nhiên liệu", 0, 100_000m, 1));

        await service.UpdateAsync(created.Id, new UpdateSurchargeDto("Nhiên liệu", (int)SurchargeCalcType.Percent, 3m, 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal((int)SurchargeCalcType.Percent, afterUpdate.CalcType);
        Assert.Equal(3m, afterUpdate.DefaultValue);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
    }
}
