using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Crm.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test <see cref="LeadService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF,
/// KHÔNG HTTP (cùng tinh thần với <c>ProviderServiceTests</c>).
/// </summary>
public class LeadServiceTests
{
    private static LeadService NewService(out FakeRepository<Lead> repo, out FakeRepository<Customer> customerRepo)
    {
        repo = new FakeRepository<Lead>();
        customerRepo = new FakeRepository<Customer>();
        return new LeadService(repo, customerRepo, new CreateLeadValidator(), new UpdateLeadValidator(), new FakeCurrentUser());
    }

    private sealed class FakeCurrentUser : TourKit.Shared.Security.ICurrentUserContext
    {
        public Guid? UserId => null;
    }

    private static CreateLeadDto NewCreateDto(string name = "Nguyen Van A") =>
        new(name, "0900000000", $"{name}@x.com", "facebook", null);

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out _);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal("Nguyen Van A", dto.FullName);
        Assert.Equal(LeadStatus.New, dto.Status);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("Nguyen Van A", stored!.FullName);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_full_name_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateLeadDto("", "0900000000", null, null, null)));
    }

    [Fact]
    public async Task ConvertAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ConvertAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ConvertAsync_creates_customer_and_sets_converted_customer_id()
    {
        var service = NewService(out var repo, out var customerRepo);
        var created = await service.CreateAsync(NewCreateDto("Tran Thi B"));

        var result = await service.ConvertAsync(created.Id);

        var customer = await customerRepo.GetByIdAsync(result.CustomerId);
        Assert.NotNull(customer);
        Assert.Equal("Tran Thi B", customer!.FullName);

        var lead = await repo.GetByIdAsync(created.Id);
        Assert.Equal(LeadStatus.Won, lead!.Status);
        Assert.Equal(result.CustomerId, lead.ConvertedCustomerId);
    }

    [Fact]
    public async Task ConvertAsync_already_converted_throws_ConflictException()
    {
        var service = NewService(out _, out _);
        var created = await service.CreateAsync(NewCreateDto("Le Van C"));
        await service.ConvertAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.ConvertAsync(created.Id));
    }

    [Fact]
    public async Task ListAsync_filters_by_status_and_q()
    {
        var service = NewService(out var repo, out _);
        await repo.AddAsync(new Lead { FullName = "Nguyễn Tiềm Năng", Source = "facebook", Status = LeadStatus.Qualified });
        await repo.AddAsync(new Lead { FullName = "Trần Mất", Source = "zalo", Status = LeadStatus.Lost });
        await repo.SaveChangesAsync();

        Assert.Equal("Nguyễn Tiềm Năng", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(Status: (int)LeadStatus.Qualified))).Items).FullName);
        Assert.Equal("Trần Mất", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(Q: "zalo"))).Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_branch()
    {
        var service = NewService(out var repo, out _);
        var branchA = Guid.NewGuid();
        var creator = Guid.NewGuid();
        await repo.AddAsync(new Lead { FullName = "A", BranchId = branchA, CreatedByUserId = creator });
        await repo.AddAsync(new Lead { FullName = "B", BranchId = Guid.NewGuid() });
        await repo.SaveChangesAsync();

        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(BranchId: branchA))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(CreatedByUserId: creator))).Items).FullName);
    }

    [Fact]
    public async Task GetStatsAsync_counts_by_status_and_converted()
    {
        var service = NewService(out var repo, out _);
        await repo.AddAsync(new Lead { FullName = "A", Status = LeadStatus.New });
        await repo.AddAsync(new Lead { FullName = "B", Status = LeadStatus.Qualified });
        await repo.AddAsync(new Lead { FullName = "C", Status = LeadStatus.Won, ConvertedCustomerId = Guid.NewGuid() });
        await repo.SaveChangesAsync();

        var stats = await service.GetStatsAsync();

        Assert.Equal(3, stats.Total);
        Assert.Equal(1, stats.New);
        Assert.Equal(1, stats.Qualified);
        Assert.Equal(1, stats.Won);
        Assert.Equal(1, stats.Converted);
        Assert.Equal(0, stats.Lost);
    }
}
