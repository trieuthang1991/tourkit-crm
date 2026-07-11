using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="PaymentTermService"/> (danh mục điều khoản thanh toán NCC).</summary>
public class PaymentTermServiceTests
{
    private static PaymentTermService NewService(out FakeRepository<PaymentTerm> repo)
    {
        repo = new FakeRepository<PaymentTerm>();
        return new PaymentTermService(repo, new CreatePaymentTermValidator(), new UpdatePaymentTermValidator());
    }

    [Fact]
    public async Task CreateAsync_persists_name_and_description()
    {
        var service = NewService(out _);

        var dto = await service.CreateAsync(new CreatePaymentTermDto("Cọc 30%", "Cọc 30%, còn lại trước khởi hành 7 ngày", 1));

        Assert.Equal("Cọc 30%", dto.Name);
        Assert.Equal("Cọc 30%, còn lại trước khởi hành 7 ngày", dto.Description);
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreatePaymentTermDto("", null, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreatePaymentTermDto("Trả trước 100%", null, 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreatePaymentTermDto("Trả trước 100%", null, 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreatePaymentTermDto("A", null, 1));

        await service.UpdateAsync(created.Id, new UpdatePaymentTermDto("B", "mô tả mới", 2));
        Assert.Equal("B", (await service.ListAsync()).Single().Name);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
    }
}
