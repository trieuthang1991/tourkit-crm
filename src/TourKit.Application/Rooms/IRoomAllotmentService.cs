using TourKit.Application.Common;
using TourKit.Application.Rooms.Dtos;

namespace TourKit.Application.Rooms;

public interface IRoomAllotmentService
{
    Task<PagedResult<RoomAllotmentDto>> ListAsync(int page, int size, RoomAllotmentListFilter? filter = null);
    Task<RoomAllotmentStatsDto> GetStatsAsync(RoomAllotmentListFilter? filter = null);
    Task<RoomAllotmentDto> GetAsync(Guid id);
    Task<RoomAllotmentDto> CreateAsync(CreateRoomAllotmentDto dto);
    Task UpdateAsync(Guid id, UpdateRoomAllotmentDto dto);
    Task DeleteAsync(Guid id);
}
