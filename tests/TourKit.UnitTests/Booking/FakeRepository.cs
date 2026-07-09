using System.Linq.Expressions;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Booking;

/// <summary>
/// Fake repo tối giản backed bởi <see cref="List{T}"/>, đủ cho unit test service (không EF).
/// <see cref="AddAsync"/> CHỈ stage vào <c>_pendingAdds</c> — giống EF thật (entity Added chưa flush DB
/// thì query non-tracking khác không thấy) — để service test được các luồng đọc-trước-khi-lưu đúng
/// như hành vi thật (vd guard overbooking đếm ghế ĐÃ lưu trước khi thêm ghế mới).
/// </summary>
public sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _items = [];
    private readonly List<T> _pendingAdds = [];

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
        _pendingAdds.Add(entity);
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

    public Task<int> SaveChangesAsync()
    {
        var count = _pendingAdds.Count;
        _items.AddRange(_pendingAdds);
        _pendingAdds.Clear();
        return Task.FromResult(count);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => Task.FromResult(_items.AsQueryable().Any(predicate));
}
