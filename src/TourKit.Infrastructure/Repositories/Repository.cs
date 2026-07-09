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

    public Task<T?> GetByIdAsync(Guid id) => Set.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
        => await (predicate is null ? Set : Set.Where(predicate)).AsNoTracking().ToListAsync();

    public async Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null)
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var p = page < PaginationDefaults.FirstPage ? PaginationDefaults.FirstPage : page;
        var s = size is < 1 or > PaginationDefaults.MaxPageSize ? PaginationDefaults.DefaultPageSize : size;
        var total = await q.CountAsync();
        var items = await q.AsNoTracking().OrderByDescending(e => e.CreatedAt).Skip((p - 1) * s).Take(s).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(T entity) => await Set.AddAsync(entity);
    public void Update(T entity) => Set.Update(entity);

    // Soft-delete (đánh dấu IsDeleted) — giữ đúng convention: global query filter tự ẩn bản ghi đã xoá.
    public void Remove(T entity)
    {
        entity.IsDeleted = true;
        Set.Update(entity);
    }
    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Set.AnyAsync(predicate);
}
