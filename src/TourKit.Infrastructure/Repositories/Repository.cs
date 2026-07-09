using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TourKit.Application.Common;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Constants;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Repositories;

public sealed class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    private DbSet<T> Set => db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => await (predicate is null ? Set : Set.Where(predicate)).AsNoTracking().ToListAsync(ct);

    public async Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var p = page < PaginationDefaults.FirstPage ? PaginationDefaults.FirstPage : page;
        var s = size is < 1 or > PaginationDefaults.MaxPageSize ? PaginationDefaults.DefaultPageSize : size;
        var total = await q.CountAsync(ct);
        var items = await q.AsNoTracking().OrderByDescending(e => e.CreatedAt).Skip((p - 1) * s).Take(s).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Set.AnyAsync(predicate, ct);
}
