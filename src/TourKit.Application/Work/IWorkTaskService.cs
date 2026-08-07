using TourKit.Application.Common;

namespace TourKit.Application.Work;

public interface IWorkTaskService
{
    Task<PagedResult<WorkTaskDto>> ListAsync(
        int page, int size, Guid? assigneeUserId, int? status, string? q = null, int? priority = null,
        bool mineScope = false);
    Task<WorkTaskStatsDto> GetStatsAsync(bool mineScope = false);
    Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto);
    Task UpdateAsync(Guid id, UpdateWorkTaskDto dto);

    /// <summary>
    /// Kéo–thả trên bảng Kanban: CHỈ đổi trạng thái, giữ nguyên mọi trường khác.
    /// Tách riêng khỏi <see cref="UpdateAsync"/> để trang không phải đọc rồi ghi lại cả bản ghi —
    /// làm vậy dễ ghi đè mất thay đổi của người khác giữa hai thao tác.
    /// Kéo sang cột Hoàn thành thì tiến độ tự về 100%; kéo ra khỏi Hoàn thành thì hạ xuống 99%
    /// để tiến độ không mâu thuẫn với trạng thái đang hiển thị.
    /// </summary>
    Task MoveAsync(Guid id, int status);

    Task DeleteAsync(Guid id);
}
