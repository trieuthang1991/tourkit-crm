using TourKit.Application.Audit.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Audit;

public interface IActivityLogService
{
    Task<PagedResult<ActivityLogDto>> ListAsync(int page, int size, string? entityName, string? entityId);
}
