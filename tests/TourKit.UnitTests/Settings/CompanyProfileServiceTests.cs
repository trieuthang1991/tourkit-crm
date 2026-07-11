using TourKit.Application.Common;
using TourKit.Application.Settings;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Settings;

/// <summary>Test <see cref="CompanyProfileService"/> — hồ sơ công ty singleton (legacy Config).</summary>
public class CompanyProfileServiceTests
{
    private static CompanyProfileService NewService(out FakeRepository<CompanyProfile> repo)
    {
        repo = new FakeRepository<CompanyProfile>();
        return new CompanyProfileService(repo);
    }

    private static CompanyProfileDto Dto(string name = "Công ty Du lịch ABC") =>
        new(name, "ABC Travel", "123 Lê Lợi", "1900 1234", "info@abc.vn", "abc.vn",
            "0301234567", "Nguyễn Văn A", "Giám đốc", "GP-123/TCDL", "VCB 0011000123456");

    [Fact]
    public async Task GetAsync_when_unset_returns_empty()
    {
        var service = NewService(out _);

        var dto = await service.GetAsync();

        Assert.Equal(string.Empty, dto.Name);
    }

    [Fact]
    public async Task SaveAsync_creates_then_GetAsync_returns_it()
    {
        var service = NewService(out _);

        await service.SaveAsync(Dto());
        var dto = await service.GetAsync();

        Assert.Equal("Công ty Du lịch ABC", dto.Name);
        Assert.Equal("0301234567", dto.TaxCode);
        Assert.Equal("Nguyễn Văn A", dto.LegalRepName);
    }

    [Fact]
    public async Task SaveAsync_twice_upserts_single_row()
    {
        var service = NewService(out var repo);

        await service.SaveAsync(Dto("Tên 1"));
        await service.SaveAsync(Dto("Tên 2"));

        Assert.Single(await repo.ListAsync());
        Assert.Equal("Tên 2", (await service.GetAsync()).Name);
    }

    [Fact]
    public async Task SaveAsync_blank_name_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.SaveAsync(Dto(" ")));
    }
}
