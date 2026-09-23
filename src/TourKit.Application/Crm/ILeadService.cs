using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> ListAsync(int page, int size, LeadListFilter? filter = null);
    Task<LeadStatsDto> GetStatsAsync();
    Task<LeadFilterOptionsDto> GetFilterOptionsAsync();
    Task<LeadDto> GetAsync(Guid id);

    /// <summary>
    /// Khách tiềm năng ĐÃ SINH RA khách hàng này; <c>null</c> nếu khách hàng không đến từ lead nào.
    ///
    /// Tra ngược để nối lại lịch sử: chuyển đổi tạo một bản ghi MỚI, nên mọi thứ gắn theo bản ghi
    /// (đánh giá AI, trao đổi) ở lại bên khách tiềm năng và trang khách hàng trắng trơn — đúng lúc
    /// khách trở nên quan trọng nhất thì lại mất sạch những gì đã biết về họ.
    /// </summary>
    Task<LeadDto?> FindByConvertedCustomerAsync(Guid customerId);
    Task<LeadDto> CreateAsync(CreateLeadDto dto);
    Task UpdateAsync(Guid id, UpdateLeadDto dto);
    Task DeleteAsync(Guid id);
    Task<ConvertLeadResultDto> ConvertAsync(Guid id, Guid? assignedToUserId = null);
}
