using System.Linq.Expressions;
using TourKit.Shared.Entities;

namespace TourKit.Application.Common;

/// <summary>Repository chung cho aggregate/entity. Query phức tạp → thêm interface repo riêng.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
