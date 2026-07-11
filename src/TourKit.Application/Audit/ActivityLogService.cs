using System.Linq.Expressions;
using TourKit.Application.Audit.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Audit;

/// <summary>Nhật ký thao tác — chỉ đọc (list phân trang, lọc theo entity). Ghi do interceptor lo.</summary>
public sealed class ActivityLogService(IRepository<ActivityLog> repo) : IActivityLogService
{
    public async Task<PagedResult<ActivityLogDto>> ListAsync(int page, int size, string? entityName, string? entityId)
    {
        Expression<Func<ActivityLog, bool>>? predicate = null;
        if (entityName is not null && entityId is not null)
        {
            predicate = a => a.EntityName == entityName && a.EntityId == entityId;
        }
        else if (entityName is not null)
        {
            predicate = a => a.EntityName == entityName;
        }
        else if (entityId is not null)
        {
            predicate = a => a.EntityId == entityId;
        }

        var (items, total) = await repo.PageAsync(page, size, predicate);
        return new PagedResult<ActivityLogDto>(items.Select(Map).ToList(), total, page, size);
    }

    private static ActivityLogDto Map(ActivityLog a) =>
        new(a.Id, a.UserId, a.Action, a.EntityName, a.EntityId, a.Changes, a.CreatedAt);
}
