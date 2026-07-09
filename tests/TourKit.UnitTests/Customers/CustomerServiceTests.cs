using System.Linq.Expressions;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Shared.Entities;

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

        public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            return Task.FromResult<IReadOnlyList<T>>(query.ToList());
        }

        public Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            var list = query.ToList();
            var pageItems = list.Skip((page - 1) * size).Take(size).ToList();
            return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, list.Count));
        }

        public Task AddAsync(T entity, CancellationToken ct = default)
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

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_items.AsQueryable().Any(predicate));
    }

    private static CustomerService NewService(out FakeRepository<Customer> repo)
    {
        repo = new FakeRepository<Customer>();
        return new CustomerService(repo, new CreateCustomerValidator(), new UpdateCustomerValidator());
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
