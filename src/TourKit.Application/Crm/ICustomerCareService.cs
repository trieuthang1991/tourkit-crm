using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ICustomerCareService
{
    Task<PagedResult<CustomerCareDto>> ListAsync(int page, int size, CustomerCareListFilter? filter = null);
    Task<CustomerCareStatsDto> GetStatsAsync();
    Task<CustomerCareDto> GetAsync(Guid id);
    Task<CustomerCareDto> CreateAsync(CreateCustomerCareDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerCareDto dto);

    /// <summary>
    /// Kéo–thả trên bảng Kanban: CHỈ đổi trạng thái chăm sóc, giữ nguyên mọi trường khác.
    /// Tách khỏi <see cref="UpdateAsync"/> để trang không phải đọc rồi ghi lại cả bản ghi —
    /// làm vậy dễ ghi đè mất phản hồi/nhắc hẹn người khác vừa sửa.
    /// </summary>
    Task MoveAsync(Guid id, int status);

    Task DeleteAsync(Guid id);
}
