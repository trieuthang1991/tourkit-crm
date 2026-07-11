using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IRoomClassService
{
    Task<IReadOnlyList<RoomClassDto>> ListAsync();
    Task<RoomClassDto> CreateAsync(CreateRoomClassDto dto);
    Task UpdateAsync(Guid id, UpdateRoomClassDto dto);
    Task DeleteAsync(Guid id);
}
