using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using ProviderCrudService = TourKit.Application.Providers.ProviderService;

namespace TourKit.UnitTests.Providers;

/// <summary>Test lọc + thống kê danh sách NCC của Provider CRUD service (bí danh tránh trùng entity ProviderService).</summary>
public class ProviderCrudServiceTests
{
    private static ProviderCrudService NewService(out FakeRepository<Provider> repo)
    {
        repo = new FakeRepository<Provider>();
        return new ProviderCrudService(repo, new CreateProviderValidator(), new UpdateProviderValidator());
    }

    private static Provider P(string code, string name, ProviderType type, int status = 1, string? phone = null, string? contact = null)
        => new() { Code = code, Name = name, Type = type, Status = status, Phone = phone, ContactPerson = contact };

    [Fact]
    public async Task ListAsync_filters_by_type_status_and_q()
    {
        var service = NewService(out var repo);
        await repo.AddAsync(P("H1", "Khách sạn ABC", ProviderType.Hotel, 1, "0900000001", "Anh Nam"));
        await repo.AddAsync(P("T1", "Vận tải XYZ", ProviderType.Vehicle, 0, "0911111111"));
        await repo.SaveChangesAsync();

        Assert.Equal("H1", Assert.Single((await service.ListAsync(1, 20, new ProviderListFilter(Type: (int)ProviderType.Hotel))).Items).Code);
        Assert.Equal("T1", Assert.Single((await service.ListAsync(1, 20, new ProviderListFilter(Status: 0))).Items).Code);
        Assert.Equal("H1", Assert.Single((await service.ListAsync(1, 20, new ProviderListFilter(Q: "Nam"))).Items).Code);  // người liên hệ
        Assert.Equal("H1", Assert.Single((await service.ListAsync(1, 20, new ProviderListFilter(Q: "ABC"))).Items).Code);  // tên
    }

    [Fact]
    public async Task GetStatsAsync_counts_active_inactive()
    {
        var service = NewService(out var repo);
        await repo.AddAsync(P("A", "A", ProviderType.Hotel, 1));
        await repo.AddAsync(P("B", "B", ProviderType.Hotel, 1));
        await repo.AddAsync(P("C", "C", ProviderType.Hotel, 0));
        await repo.SaveChangesAsync();

        var stats = await service.GetStatsAsync();

        Assert.Equal(3, stats.Total);
        Assert.Equal(2, stats.Active);
        Assert.Equal(1, stats.Inactive);
    }
}
