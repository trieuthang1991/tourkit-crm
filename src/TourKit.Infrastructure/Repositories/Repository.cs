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

    /// <summary>
    /// Kẹp cỡ trang về [1, MaxPageSize]. Xin QUÁ trần thì CẮT VỀ TRẦN, không rơi về cỡ mặc định:
    /// trước đây gọi <c>ListAsync(1, 500)</c> để đổ combobox lại âm thầm chỉ nhận 20 dòng, nên hơn
    /// chục màn hiện thiếu nhà cung cấp/chuyến đi mà không ai biết. Cỡ trang vô lý (&lt; 1) mới về mặc định.
    /// </summary>
    private static int PageSize(int size) => size switch
    {
        < 1 => PaginationDefaults.DefaultPageSize,
        > PaginationDefaults.MaxPageSize => PaginationDefaults.MaxPageSize,
        _ => size,
    };

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
        => await (predicate is null ? Set : Set.Where(predicate)).AsNoTracking().ToListAsync();

    public async Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null)
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var p = page < PaginationDefaults.FirstPage ? PaginationDefaults.FirstPage : page;
        var s = PageSize(size);
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
    /// <summary>
    /// Lưu, và DỊCH lỗi trùng khoá duy nhất của CSDL thành lỗi nghiệp vụ.
    ///
    /// Vì sao cần: 34 chỉ mục duy nhất trong CSDL không kèm điều kiện lọc IsDeleted, nên bản ghi đã
    /// xoá mềm vẫn chiếm chỗ mã. Hàm chặn trùng ở tầng service truy vấn qua bộ lọc toàn cục nên
    /// không nhìn thấy chúng — nó bảo "mã chưa dùng", CSDL bảo "trùng". Không dịch thì người dùng
    /// nhận 500 "Đã có lỗi xảy ra." sau khi xoá một danh mục rồi tạo lại đúng mã đó.
    ///
    /// Dịch Ở ĐÂY chứ không ở middleware: EF Core chỉ được phép sống trong tầng Infrastructure
    /// (có bài kiểm thử kiến trúc chốt điều đó). Middleware đã biết đổi ConflictException thành 409.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (LaTrungKhoaDuyNhat(ex))
        {
            throw new ConflictException(
                "Giá trị này đã tồn tại. Có thể một bản ghi đã xoá vẫn đang giữ mã/tên đó — hãy dùng giá trị khác.");
        }
    }

    /// <summary>
    /// Nhận diện vi phạm ràng buộc duy nhất, không phụ thuộc nhà cung cấp CSDL: Postgres trả mã
    /// 23505, SQLite trả "UNIQUE constraint failed".
    /// </summary>
    private static bool LaTrungKhoaDuyNhat(Exception ex)
    {
        for (var e = ex.InnerException; e is not null; e = e.InnerException)
        {
            if (e.Message.Contains("23505", StringComparison.Ordinal)
                || e.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Set.AnyAsync(predicate);
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => (predicate is null ? Set : Set.Where(predicate)).CountAsync();

    /// <summary>Cắt trang ở SQL theo khoá sắp xếp truyền vào (thay vì ép CreatedAt giảm dần).</summary>
    public async Task<(IReadOnlyList<T> Items, int Total)> PageAsync<TKey>(
        int page, int size, Expression<Func<T, TKey>> orderBy, bool descending,
        Expression<Func<T, bool>>? predicate = null)
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var p = page < PaginationDefaults.FirstPage ? PaginationDefaults.FirstPage : page;
        var s = PageSize(size);
        var total = await q.CountAsync();
        var ordered = descending ? q.OrderByDescending(orderBy) : q.OrderBy(orderBy);
        var items = await ordered.AsNoTracking().Skip((p - 1) * s).Take(s).ToListAsync();
        return (items, total);
    }

    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? predicate = null)
        => (predicate is null ? Set : Set.Where(predicate)).SumAsync(selector);

    public Task<int> SumIntAsync(Expression<Func<T, int>> selector, Expression<Func<T, bool>>? predicate = null)
        => (predicate is null ? Set : Set.Where(predicate)).SumAsync(selector);

    public async Task<IReadOnlyDictionary<TKey, int>> CountByAsync<TKey>(
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, bool>>? predicate = null)
        where TKey : notnull
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var rows = await q.GroupBy(keySelector)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        return rows.ToDictionary(r => r.Key, r => r.Count);
    }


}
