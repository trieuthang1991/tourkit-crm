using System.Linq.Expressions;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests;

/// <summary>
/// Repo giả DUY NHẤT cho unit test service (không EF, không CSDL).
///
/// Trước đây có ELEVEN bản sao gần như y hệt, mỗi thư mục test một bản. Hai hậu quả:
///
/// 1. Mỗi lần thêm phương thức vào <see cref="IRepository{T}"/> là phải sửa 11 chỗ.
/// 2. Tệ hơn: chúng ĐÃ TRÔI LỆCH NHAU. Tám bản phân trang theo THỨ TỰ CHÈN, trong khi
///    <c>Repository&lt;T&gt;</c> thật sắp theo <c>CreatedAt</c> GIẢM DẦN. Nghĩa là các bài kiểm thử
///    dùng tám bản đó đang xác nhận một hành vi hệ thống thật không có — bài khẳng định "trang đầu
///    chứa X" có thể xanh ở đây mà sai khi chạy thật. Bản ở thư mục Collaboration còn ghi chú thích
///    rằng nó cố ý khác các bản kia: người viết biết có lệch nhưng chỉ sửa cho chỗ mình cần.
///
/// Bản gộp này theo hành vi ĐÚNG. Đặt ở namespace gốc <c>TourKit.UnitTests</c> nên mọi thư mục con
/// thấy được mà không cần khai using.
/// </summary>
public sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _items = [];
    private readonly List<T> _pendingAdds = [];

    /// <summary>Nạp sẵn dữ liệu cho test, bỏ qua vòng AddAsync/SaveChanges.</summary>
    public void Seed(params T[] entities) => _items.AddRange(entities);

    public IReadOnlyList<T> Items => _items;

    /// <summary>
    /// Tổng số dòng đã NẠP VỀ BỘ NHỚ qua <see cref="ListAsync"/> — thước đo duy nhất phân biệt
    /// "cắt trang ở SQL" với "nạp cả bảng rồi cắt trong bộ nhớ". Không có nó thì tối ưu xong không ai
    /// chứng minh được là đã nhanh hơn, và lần refactor sau rất dễ đưa mọi thứ về như cũ mà bộ kiểm
    /// thử vẫn xanh. <see cref="PageAsync(int,int,Expression{Func{T,bool}})"/> KHÔNG cộng vào đây:
    /// nó cắt trang trước khi trả về, đúng thứ ta muốn.
    /// </summary>
    public int SoDongDaNap { get; private set; }

    public Task<T?> GetByIdAsync(Guid id)
        => Task.FromResult(_items.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
        var ds = query.ToList();
        SoDongDaNap += ds.Count;
        return Task.FromResult<IReadOnlyList<T>>(ds);
    }

    /// <summary>
    /// Bám <c>Repository.PageAsync</c> thật: SẮP THEO CreatedAt GIẢM DẦN rồi mới cắt trang.
    /// Dùng thứ tự chèn ở đây là để bài kiểm thử xanh trong khi thực tế trả sai thứ tự.
    /// </summary>
    public Task<(IReadOnlyList<T> Items, int Total)> PageAsync(
        int page, int size, Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        var total = query.Count();
        var pageItems = query.OrderByDescending(e => e.CreatedAt).Skip((page - 1) * size).Take(size).ToList();
        return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, total));
    }

    public Task<(IReadOnlyList<T> Items, int Total)> PageAsync<TKey>(
        int page, int size, Expression<Func<T, TKey>> orderBy, bool descending,
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        var total = query.Count();
        var ordered = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var pageItems = ordered.Skip((page - 1) * size).Take(size).ToList();
        return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, total));
    }

    public Task<(IReadOnlyList<T> Items, int Total)> PageAsync<TKey1, TKey2>(
        int page, int size,
        Expression<Func<T, TKey1>> orderBy, bool descending,
        Expression<Func<T, TKey2>> thenBy, bool thenDescending,
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        var total = query.Count();
        var b1 = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var b2 = thenDescending ? b1.ThenByDescending(thenBy) : b1.ThenBy(thenBy);
        var pageItems = b2.Skip((page - 1) * size).Take(size).ToList();
        return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, total));
    }

    public Task<(IReadOnlyList<T> Items, int Total)> PageAsync<TKey1, TKey2, TKey3>(
        int page, int size,
        Expression<Func<T, TKey1>> orderBy, bool descending,
        Expression<Func<T, TKey2>> thenBy, bool thenDescending,
        Expression<Func<T, TKey3>> thenBy2, bool thenDescending2,
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        var total = query.Count();
        var b1 = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var b2 = thenDescending ? b1.ThenByDescending(thenBy) : b1.ThenBy(thenBy);
        var b3 = thenDescending2 ? b2.ThenByDescending(thenBy2) : b2.ThenBy(thenBy2);
        var pageItems = b3.Skip((page - 1) * size).Take(size).ToList();
        return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, total));
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

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => Task.FromResult(predicate is null ? _items.Count : _items.AsQueryable().Count(predicate));

    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        return Task.FromResult(query.Sum(selector));
    }

    public Task<int> SumIntAsync(Expression<Func<T, int>> selector, Expression<Func<T, bool>>? predicate = null)
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        return Task.FromResult(query.Sum(selector));
    }

    public Task<IReadOnlyDictionary<TKey, int>> CountByAsync<TKey>(
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, bool>>? predicate = null)
        where TKey : notnull
    {
        var query = predicate is null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
        IReadOnlyDictionary<TKey, int> result = query.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult(result);
    }
}
