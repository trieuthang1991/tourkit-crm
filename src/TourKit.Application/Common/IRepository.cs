using System.Linq.Expressions;
using TourKit.Shared.Entities;

namespace TourKit.Application.Common;

/// <summary>Repository chung cho aggregate/entity. Query phức tạp → thêm interface repo riêng.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null);
    Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Đếm ở SQL (COUNT) — KHÔNG materialize bảng.</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Cắt trang ở SQL với KHOÁ SẮP XẾP tuỳ chọn. Bản <see cref="PageAsync(int,int,Expression{Func{T,bool}})"/>
    /// ép sắp theo CreatedAt giảm dần, không dùng được cho màn cần sắp theo cột nghiệp vụ
    /// (vd hoá đơn sắp theo ngày hoá đơn) — trước đây các màn đó buộc phải nạp cả bảng rồi sắp ở bộ nhớ.
    /// </summary>
    Task<(IReadOnlyList<T> Items, int Total)> PageAsync<TKey>(
        int page, int size, Expression<Func<T, TKey>> orderBy, bool descending,
        Expression<Func<T, bool>>? predicate = null);

    /// <summary>Cộng ở SQL (SUM) — KHÔNG materialize bảng. Dùng cho thẻ thống kê có tổng tiền.</summary>
    Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? predicate = null);

    /// <summary>Cộng cột SỐ NGUYÊN ở SQL (vd tổng số chỗ) — bản decimal không nhận int.</summary>
    Task<int> SumIntAsync(Expression<Func<T, int>> selector, Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Đếm theo NHÓM bằng một câu GROUP BY duy nhất ở SQL. Dành cho thẻ thống kê dạng
    /// "tổng + đếm theo trạng thái": trước đây mỗi thẻ hoặc nạp cả bảng rồi đếm trong bộ nhớ,
    /// hoặc bắn 5-6 câu COUNT riêng lẻ. Cả hai đều thừa — một GROUP BY trả về đủ mọi bậc.
    /// </summary>
    Task<IReadOnlyDictionary<TKey, int>> CountByAsync<TKey>(
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, bool>>? predicate = null)
        where TKey : notnull;


}
