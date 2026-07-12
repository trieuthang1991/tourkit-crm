using System.Linq.Expressions;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Customers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.UnitTests.Customers;

/// <summary>
/// Test <see cref="CustomerService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP (cùng tinh thần với các slice test khác trong assembly).
/// </summary>
public class CustomerServiceTests
{
    /// <summary>Fake repo tối giản backed bởi <see cref="List{T}"/>, đủ cho unit test service.</summary>
    private sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items = [];

        public Task<T?> GetByIdAsync(Guid id)
            => Task.FromResult(_items.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            return Task.FromResult<IReadOnlyList<T>>(query.ToList());
        }

        public Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            var list = query.ToList();
            var pageItems = list.Skip((page - 1) * size).Take(size).ToList();
            return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, list.Count));
        }

        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            var index = _items.FindIndex(e => e.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }
        }

        public void Remove(T entity) => _items.RemoveAll(e => e.Id == entity.Id);

        public Task<int> SaveChangesAsync() => Task.FromResult(0);

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => Task.FromResult(_items.AsQueryable().Any(predicate));
    }

    private sealed class FakeCurrentUser : ICurrentUserContext
    {
        public Guid? UserId => null;
    }

    private static CustomerService NewService(out FakeRepository<Customer> repo)
    {
        repo = new FakeRepository<Customer>();
        return new CustomerService(
            repo, new FakeRepository<Order>(), new FakeRepository<CustomerCare>(), new FakeRepository<User>(),
            new FakeCurrentUser(), new CreateCustomerValidator(), new UpdateCustomerValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCustomerDto("Nguyen Van A", "0900000000"));

        Assert.Equal("Nguyen Van A", dto.FullName);
        Assert.Equal("0900000000", dto.Phone);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("Nguyen Van A", stored!.FullName);
    }

    [Fact]
    public async Task CreateAsync_persists_passport_and_personal_fields()
    {
        var service = NewService(out var repo);
        var expiry = DateTimeOffset.UtcNow.AddYears(5);

        var dto = await service.CreateAsync(new CreateCustomerDto(
            "Trần B", "0911111111",
            Email: "b@x.com", Address: "Hà Nội", IdCardNumber: "0123456789",
            PassportNumber: "B1234567", PassportExpiry: expiry, Nationality: "Việt Nam"));

        Assert.Equal("b@x.com", dto.Email);
        Assert.Equal("B1234567", dto.PassportNumber);
        Assert.Equal("Việt Nam", dto.Nationality);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.Equal("B1234567", stored!.PassportNumber);
        Assert.Equal(expiry, stored.PassportExpiry);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_FullName_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCustomerDto("", null)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerDto("Ten moi", null)));
    }
}
