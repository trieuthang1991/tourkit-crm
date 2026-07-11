using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="PaymentAccountService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class PaymentAccountServiceTests
{
    private static PaymentAccountService NewService(out FakeRepository<PaymentAccount> repo)
    {
        repo = new FakeRepository<PaymentAccount>();
        return new PaymentAccountService(repo, new CreatePaymentAccountValidator(), new UpdatePaymentAccountValidator());
    }

    private static CreatePaymentAccountDto NewDto(string name, bool isDefault = false) =>
        new(name, "Vietcombank", "0123456789", "CÔNG TY ABC", "CN Hà Nội", "Thanh toan tour", isDefault, 1);

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_bank_fields()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(NewDto("VCB - Cty ABC"));

        Assert.Equal("VCB - Cty ABC", dto.Name);
        Assert.Equal("Vietcombank", dto.BankName);
        Assert.Equal("0123456789", dto.AccountNumber);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto("")));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(NewDto("VCB"));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto("VCB")));
    }

    [Fact]
    public async Task Creating_second_default_clears_the_first_default()
    {
        var service = NewService(out _);
        var first = await service.CreateAsync(NewDto("VCB", isDefault: true));
        var second = await service.CreateAsync(NewDto("Techcombank", isDefault: true));

        var list = await service.ListAsync();
        Assert.True(list.Single(x => x.Id == second.Id).IsDefault);
        Assert.False(list.Single(x => x.Id == first.Id).IsDefault); // mặc định cũ bị gỡ
        Assert.Single(list, x => x.IsDefault);                       // đúng 1 mặc định
    }

    [Fact]
    public async Task Updating_account_to_default_clears_other_default()
    {
        var service = NewService(out _);
        var first = await service.CreateAsync(NewDto("VCB", isDefault: true));
        var second = await service.CreateAsync(NewDto("ACB", isDefault: false));

        await service.UpdateAsync(second.Id, new UpdatePaymentAccountDto("ACB", "ACB", "999", "ABC", null, null, true, 2));

        var list = await service.ListAsync();
        Assert.False(list.Single(x => x.Id == first.Id).IsDefault);
        Assert.True(list.Single(x => x.Id == second.Id).IsDefault);
        Assert.Single(list, x => x.IsDefault);
    }

    [Fact]
    public async Task DeleteAsync_removes_account()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(NewDto("VCB"));

        await service.DeleteAsync(created.Id);

        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdatePaymentAccountDto("X", null, null, null, null, null, false, 1)));
    }
}
