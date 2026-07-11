using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IGuideAssignmentService
{
    Task<PagedResult<GuideAssignmentDto>> ListAsync(int page, int size, Guid? departureId);
    Task<GuideAssignmentDto> CreateAsync(CreateGuideAssignmentDto dto);
    Task UpdateAsync(Guid id, UpdateGuideAssignmentDto dto);
    Task<GuideAssignmentDto> HandoverAsync(Guid id, HandoverDto dto);
    Task DeleteAsync(Guid id);
}
